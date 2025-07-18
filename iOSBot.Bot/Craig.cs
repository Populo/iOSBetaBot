﻿using System.Text;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Bot.Commands;
using iOSBot.Bot.Models;
using iOSBot.Bot.Quartz;
using iOSBot.Data;
using iOSBot.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using IChannel = RabbitMQ.Client.IChannel;
using IConnection = RabbitMQ.Client.IConnection;
using ITrigger = Quartz.ITrigger;

namespace iOSBot.Bot;

public class Craig
{
    private readonly DiscordSocketClient _client;
    private readonly ICraigService _craigService;

    private readonly IDiscordService _discordService;
    // https://discord.com/api/oauth2/authorize?client_id=1126703029618475118&permissions=3136&redirect_uri=https%3A%2F%2Fgithub.com%2FPopulo%2FiOSBetaBot&scope=bot

    private readonly ILogger<Craig> _logger;
    private readonly string _tier;

    private IJobDetail _job;
    private IChannel _mqChannel;

    private IConnection _mqConnection;
    private AsyncEventingBasicConsumer _mqConsumer;

    private IScheduler _scheduler;
    private IServiceProvider _services;
    private ITrigger _trigger;


    public Craig()
    {
        _services = CreateProvider();

        _logger = _services.GetRequiredService<ILogger<Craig>>()
                  ?? throw new Exception("Cannot get logger from factory");
        _client = _services.GetRequiredService<DiscordSocketClient>()
                  ?? throw new Exception("Cannot get client from factory");
        _discordService = _services.GetRequiredService<IDiscordService>()
                          ?? throw new Exception("Cannot get discord service from factory");
        _craigService = _services.GetRequiredService<ICraigService>()
                        ?? throw new Exception("Cannot get craig service from factory");

        _tier = _craigService.GetTier();
    }

    public static Task Main(string[] args) => new Craig().Run(args);

    private async Task Run(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            _logger.LogError("{UnhandledExceptionEventArgs}\n{EExceptionObject}", e, e.ExceptionObject);
            _discordService.PostError($"Unhandled exception: {e.ExceptionObject}");
        };

        var token = (await File.ReadAllTextAsync("/run/secrets/botToken")).Trim();

        _logger.LogInformation("Environment: {Tier}", _tier);

        _client.Ready += ClientOnReady;
        _client.Log += ClientOnLog;
        _client.SlashCommandExecuted += ClientOnSlashCommandExecuted;
        _client.MessageReceived += ClientOnMessageReceived;
        _client.JoinedGuild += ClientOnJoinedGuild;
        _client.LeftGuild += ClientOnLeftGuild;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.SetCustomStatusAsync(_craigService.GetVersion().ToString());
        await _client.StartAsync();


        _logger.LogInformation("Started");
        await Task.Delay(-1);
    }

    private async Task ClientOnLeftGuild(SocketGuild guild)
    {
        await _discordService.PostError($"Craig has been removed from {guild.Name}");
        await _discordService.NukeServer(guild.Id);
    }

    private async Task ClientOnReady()
    {
        //_ = RunBulkCommand();
        await AdminCommands.UpdateCommands(_client, null, true);

        _scheduler = await StdSchedulerFactory.GetDefaultScheduler();
        _scheduler.JobFactory =
            new MicrosoftDependencyInjectionJobFactory(_services, new OptionsWrapper<QuartzOptions>(null));
        var secondDelay = _craigService.GetSecondsDelay();
        await _scheduler.Start();
        _job = JobBuilder.Create<InternJob>()
            .WithIdentity("CraigsInternJob", "Intern")
            .Build();
        _trigger = TriggerBuilder.Create()
            .WithIdentity("CraigsInternTrigger", "Intern")
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(secondDelay).RepeatForever())
            .Build();
        await _scheduler.ScheduleJob(_job, _trigger);

        // prod: 1126703029618475118
        // dev: 1133469416458301510
        _logger.LogInformation($"Bot UserID: {_client.CurrentUser.Id}");

        await ConnectMqAsync();
    }

    private async Task ConnectMqAsync()
    {
        // Reconnect using the same setup code
        var isProd = _tier == "Prod";
        var mqUsername = isProd ? "CraigBot" : "CraigBotDev";
        var mqQueue = isProd ? "updates-queue" : "updates-queue-dev";

        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            UserName = mqUsername,
            Password = (await File.ReadAllTextAsync("/run/secrets/mqPass")).Trim()
        };

        _mqConnection = await factory.CreateConnectionAsync();
        _mqChannel = await _mqConnection.CreateChannelAsync();
        await _mqChannel.ExchangeDeclareAsync("updates-exchange", type: ExchangeType.Fanout, durable: true);

        var queueDeclare = await _mqChannel.QueueDeclareAsync(
            queue: mqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await _mqChannel.QueueBindAsync(
            queue: queueDeclare.QueueName,
            exchange: "updates-exchange",
            routingKey: mqQueue);

        _mqConsumer = new AsyncEventingBasicConsumer(_mqChannel);
        _mqConsumer.ReceivedAsync += ConsumerOnReceivedAsync;
        _mqConnection.ConnectionShutdownAsync += MqConnectionOnExceptionListener;


        await _mqChannel.BasicConsumeAsync(
            queue: queueDeclare.QueueName,
            autoAck: true,
            consumer: _mqConsumer);
    }

    private async Task ConsumerOnReceivedAsync(object sender, BasicDeliverEventArgs @event)
    {
        var body = @event.Body.ToArray();
        var textMsg = Encoding.UTF8.GetString(body);

        if (textMsg == null) throw new Exception("Could not read message");

        var update = JsonConvert.DeserializeObject<UpdateDto>(textMsg);
        if (null == update) throw new Exception("Could not parse message");
        await using var craigDb = new BetaContext();

        var servers = craigDb.Servers.Where(s => s.Track == update.TrackId);

        _logger.LogInformation(
            "Update for {UpdateTrackName} found. Version {UpdateVersion} with build id {UpdateBuild}", update.TrackName,
            update.Version, update.Build);

        // dont post if older than 12 hours, still add to db tho
        var postOld = Convert.ToBoolean(craigDb.Configs.First(c => c.Name == "PostOld").Value);

        if (!postOld && update.ReleaseDate.DayOfYear != DateTime.Today.DayOfYear)
        {
            var error =
                $"{update.TrackName} update {update.Version}-{update.Build} was released on {update.ReleaseDate.ToShortDateString()}. too old. not posting.";
            _logger.LogInformation(error);
            await _discordService.PostError(error);

            return;
        }

        var up = update.ConvertUpdate();

        foreach (var server in servers)
        {
            try
            {
                await _craigService.PostUpdateNotification(server, up);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error posting update to server {ServerId}", server.Id);
                await _discordService.PostError($"Error posting update to server {server.Id}\n{e.Message}");
            }
        }
    }

    private async Task ReconnectMqAsync()
    {
        try
        {
            // Clean up existing resources
            await DisposeMqResourcesAsync();

            await ConnectMqAsync();

            _logger.LogInformation("Successfully reconnected to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in reconnection attempt");
            throw;
        }
    }

    private async Task MqConnectionOnExceptionListener(object sender, ShutdownEventArgs args)
    {
        _logger.LogError(args.Exception, "RabbitMQ connection error");
        // Attempt to reconnect
        await Task.Run(async () =>
        {
            try
            {
                await ReconnectMqAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to RabbitMQ");
                await _discordService.PostError("Failed to reconnect to RabbitMQ\n" + ex.Message);
            }
        });
    }

    private async Task DisposeMqResourcesAsync()
    {
        try
        {
            if (_mqChannel != null)
            {
                await _mqChannel.CloseAsync();
                _mqChannel.Dispose();
                _mqChannel = null;
            }

            if (_mqConnection != null && _mqConnection.IsOpen)
            {
                await _mqConnection.CloseAsync();
                _mqConnection.Dispose();
                _mqConnection = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ resources");
        }
    }

    private async Task ClientOnJoinedGuild(SocketGuild arg)
    {
        _ = _discordService.PostError($"Craig has joined {arg.Name} adding {arg.MemberCount} members.");
        try
        {
            var me = arg.GetUser(_client.CurrentUser.Id);
            _ = me.ModifyAsync(m => { m.Nickname = "Craig"; });
        }
        catch
        {
            _logger.LogError("Could not change nickname to Craig");
        }

        var owner = await _client.GetUserAsync(arg.OwnerId);
        if (null == owner) return;

        var message = "Hello!\nThank you for inviting Craig to your server.\n" +
                      "To get started, use /watch in the channel you want to see updates posted in. " +
                      "You will be given various update tracks to choose from. " +
                      "You can add as many or as few as you like.\n" +
                      "/yesthread will tell Craig to create a discussion thread for the specified update in the channel.\n" +
                      "/yesforum will tell him to create a forum post in a forum channel.\n" +
                      "/nothread and /noforum will undo these commands.\n\n" +
                      "For support you can join my dedicated bot server: https://discord.gg/NX6nYrNtbU, " +
                      "message my creator (@populo), or DM me in this chat directly.\n" +
                      "Thanks again!";

        _ = owner.SendMessageAsync(message);
    }

    private async Task ClientOnMessageReceived(SocketMessage arg)
    {
        if (arg.Author.IsBot) return;

        await using var db = new BetaContext();
        var dmForum =
            await _client.GetChannelAsync(ulong.Parse(db.Configs.First(c => c.Name == "DMForum").Value)) as
                SocketForumChannel
            ?? throw new Exception("Cannot get DM Forum from Id provided from database");

        if (arg.Channel is IThreadChannel threadChannel)
        {
            var restThread = await _client.Rest.GetChannelAsync(threadChannel.Id) as RestThreadChannel
                             ?? throw new Exception("Cannot get thread from rest api");
            if (restThread.ParentChannelId != dmForum.Id) return;
        }
        else if (arg.Channel is not IDMChannel) return;

        var channels = await dmForum.GetActiveThreadsAsync()
                       ?? throw new Exception("Cannot get active threads from rest api");

        if (arg.Channel is IDMChannel)
        {
            var post = channels.FirstOrDefault(t => t.Name.Contains(arg.Author.Id.ToString())) as IThreadChannel;

            if (null == post)
            {
                var embed = new EmbedBuilder()
                {
                    Title = $"DMs between Craig and {arg.Author}",
                    ThumbnailUrl = arg.Author.GetAvatarUrl(),
                };
                post = await dmForum.CreatePostAsync($"Craig DM @{arg.Author.Username} - {arg.Author.Id}",
                    ThreadArchiveDuration.OneWeek, embed: embed.Build());
                await arg.Channel.SendMessageAsync($"Your message has been sent. Someone will be with you shortly.");
            }

            if (post.IsArchived)
                await post.ModifyAsync(t => { t.Archived = false; });

            await post.SendMessageAsync($"{arg.Content}\n-@{arg.Author.Username}");
        }
        else
        {
            var userId = arg.Channel.Name.Split(' ').Last();
            var user = await _client.Rest.GetUserAsync(ulong.Parse(userId));
            await user.SendMessageAsync($"{arg.Content}\n-@{arg.Author.Username}");
        }
    }

    private async Task ClientOnSlashCommandExecuted(SocketSlashCommand arg)
    {
        if (arg.IsDMInteraction)
        {
            await arg.RespondAsync("Craig currently does not support commands over DM.");
            return;
        }

        var jsonArgs = new JObject();
        foreach (var o in arg.Data.Options)
        {
            jsonArgs.Add(null != o.Value ? new JProperty(o.Name, o.Value.ToString()) : new JProperty(o.Name, "null"));
        }

        string command = "";
        try
        {
            command =
                $"Command received: /{arg.CommandName}\nin channel: {await _client.GetChannelAsync(arg.ChannelId.Value)}\nin server: {_client.GetGuild(arg.GuildId.Value).Name}\nfrom: {arg.User.Username}\n```json\nargs:{jsonArgs}\n```";

            _logger.LogInformation(command);
        }
        catch
        {
            _logger.LogError(
                "Could not get channel or server from slash command.\nCommand received: /{ArgCommandName}\nin channel: {GetChannelAsync}",
                arg.CommandName, await _client.GetChannelAsync(arg.ChannelId.Value));
        }

        try
        {
            switch (arg.CommandName)
            {
                case "test":
                    try
                    {
                        var embed = new EmbedBuilder()
                            .WithAuthor(_client.CurrentUser)
                            .WithTitle("Test Embed")
                            .WithDescription("This is a test embed")
                            .AddField("Test Field", "GOOD MORNING and welcome to Apple Park");

                        await arg.RespondAsync("Test command executed.", embed: embed.Build());
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error executing test command.\n{ExMessage}", e.Message);
                        ;
                        await arg.RespondAsync("Failed", ephemeral: true);
                    }

                    break;
                // admin commands
                case "yeserrors":
                    _ = AdminCommands.YesErrors(arg, _client);
                    break;
                case "noerrors":
                    _ = AdminCommands.NoErrors(arg, _client);
                    break;
                // case "force":
                //     if (!AdminCommands.IsAllowed(arg.User.Id))
                //     {
                //         await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                //         return;
                //     }
                //
                //     await arg.DeferAsync(ephemeral: true);
                //
                //     //await _craigService.CheckForUpdates();
                //     await _scheduler.TriggerJob(_trigger.JobKey);
                //
                //     _logger.LogInformation("Update forced by {UserGlobalName}", arg.User.GlobalName);
                //     await arg.FollowupAsync("Updates checked.");
                //     break;
                case "update":
                    _ = AdminCommands.UpdateCommands(_client, arg);
                    break;
                case "servers":
                    _ = AdminCommands.GetServers(arg, _client);
                    break;
                case "start":
                    _ = PauseBot(arg, false);
                    break;
                case "stop":
                    _ = PauseBot(arg, true);
                    break;
                case "fake":
                    _ = AdminCommands.FakeUpdate(arg, _craigService);
                    break;
                // meme commands
                case "manifest":
                    _ = MemeCommands.Manifest(arg);
                    break;
                case "goodbot":
                    _ = MemeCommands.GoodBot(arg, _client);
                    break;
                case "badbot":
                    _ = MemeCommands.BadBot(arg, _client);
                    break;
                case "when":
                    _ = MemeCommands.When(arg);
                    break;
                case "whycraig":
                    _ = MemeCommands.WhyCraig(arg);
                    break;
                // apple commands
                case "watch":
                    _ = AppleCommands.YesWatch(arg, _client);
                    break;
                case "unwatch":
                    _ = AppleCommands.NoWatch(arg, _client);
                    break;
                case "yesthreads":
                    _ = AppleCommands.YesThreads(arg, _client);
                    break;
                case "nothreads":
                    _ = AppleCommands.NoThreads(arg);
                    break;
                case "yesforum":
                    _ = AppleCommands.YesForum(arg, _client);
                    break;
                case "noforum":
                    _ = AppleCommands.NoForum(arg, _client);
                    break;
                // misc commands
                case "whygm":
                    _ = arg.RespondAsync(
                        "GM is being used instead of RC because based on the IDs Apple gives releases, they are different than RCs." +
                        " A normal beta release has an ID similar to iOS182Beta3; an RC would have something like iOS182Short;" +
                        " while a stable update would have iOS182Long. This update has an ID ending in 'Long' despite being " +
                        "on a beta track rather than stable releases track. Therefore, is higher than an RC, but less than Stable" +
                        " Hence, Golden Master.", ephemeral: true);
                    break;
                case "craiginfo":
                    _ = arg.RespondAsync(
                        "Craig is a bot meant to track Apple OS releases, specifically the beta ones\n" +
                        "Created by: @populo\n[Support discord](https://discord.gg/NX6nYrNtbU)"
                        , ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(69, ex, "Error executing command:\n{ExMessage}\n\n{Command}", ex.Message, command);
            await _discordService.PostError($"Error executing command:\n{ex.Message}\n\n{command}");
        }
    }

    private Task ClientOnLog(LogMessage arg)
    {
        _logger.LogInformation(arg.Message);
        if (null == arg.Exception ||
            arg.Message.Contains("Server requested a reconnect") ||
            arg.Message.Contains("WebSocket connection was closed"))
        {
            return Task.CompletedTask;
        }

        _discordService.PostError(
            $"Bot error:\n{arg.Exception.Message}\n\n{arg.Exception.InnerException?.StackTrace}");
        _logger.LogError(1, arg.Exception, "Bot error:\n{ExMessage}\n\n{InnerStackTrace}", arg.Exception.Message,
            arg.Exception.InnerException?.StackTrace);

        return Task.CompletedTask;
    }

    private async Task PauseBot(SocketSlashCommand arg, bool pause)
    {
        if (!AdminCommands.IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        await arg.RespondAsync($"Craig paused: {_craigService.TogglePause(pause)}");
    }

    private IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent,
            MessageCacheSize = 15
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("./logs/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var collection = new ServiceCollection();

        collection
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddTransient<IDiscordService, DiscordService>()
            .AddTransient<ICraigService, CraigService>();

        collection.AddLogging(configuration =>
        {
            configuration.ClearProviders();
            configuration.AddSerilog();
        });

        return collection.BuildServiceProvider();
    }

    private async Task RunBulkCommand()
    {
        foreach (var server in _client.Guilds)
        {
            try
            {
                var me = server.GetUser(_client.CurrentUser.Id);
                if (me.Nickname != null)
                {
                    _logger.LogInformation("Custom nickname already set in {ServerName}: {MeNickname}", server.Name,
                        me.Nickname);
                    continue;
                }

                await me.ModifyAsync(m => { m.Nickname = "Craig"; });
                Task.Delay(1000).Wait();
                _logger.LogInformation("Changed nickname to {MeNickname} in {ServerName}", me.Nickname, server.Name);
            }
            catch
            {
                _logger.LogInformation("Could not change nickname to Craig");
            }
        }
    }
}