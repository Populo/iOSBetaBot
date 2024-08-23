﻿using System.Collections.Concurrent;
using System.Timers;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Bot.Commands;
using iOSBot.Bot.Helpers;
using iOSBot.Data;
using iOSBot.Service;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NLog;
using Timer = System.Timers.Timer;
using Update = iOSBot.Service.Update;

namespace iOSBot.Bot;

public class Craig
{
    // https://discord.com/api/oauth2/authorize?client_id=1126703029618475118&permissions=3136&redirect_uri=https%3A%2F%2Fgithub.com%2FPopulo%2FiOSBetaBot&scope=bot

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private Version _version = new(2024, 08, 20, 2);

    public Craig()
    {
        var serviceProvider = CreateProvider();

        Client = serviceProvider.GetRequiredService<DiscordSocketClient>()
                 ?? throw new Exception("Cannot get client from factory");
        AppleService = serviceProvider.GetRequiredService<IAppleService>();

        UpdatePoster = new Poster(AppleService, Client);

        PollTimer = new Timer()
        {
            AutoReset = true,
            Enabled = false,
            Interval = 1000 * 60 * 2 // 2 minutes
        };
        PollTimer.Elapsed += PollTimerOnElapsed;
    }

    private DiscordSocketClient Client { get; set; }
    private IAppleService AppleService { get; set; }
    private string? Status { get; set; }
    private Poster UpdatePoster { get; set; }
    private Timer PollTimer { get; init; }

    public static Task Main(string[] args) => new Craig().Run(args);

    public async Task Run(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => { _logger.Error($"{e}\n{e.ExceptionObject}"); };

        if (args.Length == 0) throw new Exception("Include token in args");
        switch (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
        {
            case "Release":
                _logger.Info("Environment: Prod");
                break;
            case "Develop":
                _logger.Info("Environment: Dev");
                break;
        }

        Status = $"Version: {_version}";

        Client.Ready += () =>
        {
            _ = UpdatePoster.PostError("Good morning! Welcome to Apple Park.");
            return AdminCommands.UpdateCommands(Client, null, true);
        };
        Client.Log += ClientOnLog;
        Client.SlashCommandExecuted += ClientOnSlashCommandExecuted;
        Client.MessageReceived += ClientOnMessageReceived;
        Client.ButtonExecuted += ClientOnButtonExecuted;
        Client.ModalSubmitted += ClientOnModalSubmitted;

        await Client.LoginAsync(TokenType.Bot, args[0]);
        await Client.SetCustomStatusAsync(Status);

        await Client.StartAsync();
        PollTimer.Start();

        _logger.Info("Started");
        await Task.Delay(-1);
    }

    private async Task ClientOnModalSubmitted(SocketModal arg)
    {
        if (!AdminCommands.IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this button");
            return;
        }

        if (arg.Data.CustomId.Contains("replymodal"))
        {
            var channelId = ulong.Parse(arg.Data.CustomId.Split('-').Last());
            var channel = await Client.GetDMChannelAsync(channelId);

            var textBox = arg.Data.Components.First();
            var message = textBox.Value + $"\n -@{arg.User.Username}";

            await channel.SendMessageAsync(message);
            await arg.RespondAsync($"Responded with:\n {message}");
        }
    }

    private async Task ClientOnButtonExecuted(SocketMessageComponent arg)
    {
        if (arg.Data.CustomId.Contains("reply"))
        {
            if (!AdminCommands.IsAllowed(arg.User.Id))
            {
                await arg.RespondAsync("Only the bot creator can use this button");
                return;
            }

            var channelId = ulong.Parse(arg.Data.CustomId.Split('-').Last());

            var responseBox = new TextInputBuilder()
            {
                Placeholder = "What's the reply?",
                Required = true,
                CustomId = $"textbox-{channelId}",
                Label = "Reply",
                Style = TextInputStyle.Paragraph
            };
            var responseModal = new ModalBuilder()
                {
                    Title = "Reply",
                    CustomId = $"replymodal-{channelId}"
                }
                .AddTextInput(responseBox);

            await arg.RespondWithModalAsync(responseModal.Build());
        }
    }

    private async Task ClientOnMessageReceived(SocketMessage arg)
    {
        if (arg.Author.IsBot || arg.Channel is not IDMChannel) return;

        using var db = new BetaContext();

        var channel = arg.Channel as IDMChannel
                      ?? throw new Exception("Cannot get channel from DM");
        await channel.SendMessageAsync("Thank you for your message, Someone will be with you shortly.");

        var message = new EmbedBuilder()
            {
                Title = "New DM Received"
            }
            .AddField("Sender", arg.Author.Username)
            .AddField("Message", arg.Content)
            .WithThumbnailUrl(arg.Author.GetAvatarUrl())
            .WithColor(Color.Gold);

        var button = new ComponentBuilder()
            .WithButton("Reply", $"reply-{channel.Id}");

        foreach (var s in db.ErrorServers)
        {
            var c = await Client.GetChannelAsync(s.ChannelId) as RestTextChannel
                    ?? throw new Exception("Cannot get error channel.");
            await c.SendMessageAsync(components: button.Build(), embed: message.Build());
        }
    }

    private async Task ClientOnSlashCommandExecuted(SocketSlashCommand arg)
    {
        var jsonArgs = new JObject();
        foreach (var o in arg.Data.Options)
        {
            if (null != o.Value) jsonArgs.Add(new JProperty(o.Name, o.Value.ToString()));
            else jsonArgs.Add(new JProperty(o.Name, "null"));
        }

        _logger.Info(
            $"Command received: /{arg.CommandName}\nin channel: {await Client.GetChannelAsync(arg.ChannelId!.Value)}\nin server: {Client.GetGuild(arg.GuildId!.Value).Name}\nfrom: {arg.User.Username}\n```json\nargs:{jsonArgs}\n```");

        switch (arg.CommandName)
        {
            // admin commands
            case "yeserrors":
                await AdminCommands.YesErrors(arg, Client);
                break;
            case "noerrors":
                await AdminCommands.NoErrors(arg, Client);
                break;
            case "force":
                if (!AdminCommands.IsAllowed(arg.User.Id))
                {
                    await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                    return;
                }

                await arg.DeferAsync(ephemeral: true);

                // try to prevent what looks like some race conditions
                PollTimer.Stop();
                PollTimer.Start();

                PollTimerOnElapsed(null, null!);

                _logger.Info($"Update forced by {arg.User.GlobalName}");
                await arg.FollowupAsync("Updates checked.");
                break;
            case "update":
                await AdminCommands.UpdateCommands(Client, arg);
                break;
            case "servers":
                await AdminCommands.GetServers(arg, Client);
                break;
            case "start":
                await PauseBot(arg, false);
                break;
            case "stop":
                await PauseBot(arg, true);
                break;
            case "fake":
                await AdminCommands.FakeUpdate(arg, UpdatePoster);
                break;
            case "toggle":
                await AdminCommands.ToggleDevice(arg);
                break;
            // meme commands
            case "manifest":
                await MemeCommands.Manifest(arg);
                break;
            case "goodbot":
                await MemeCommands.GoodBot(arg, Client.Rest);
                break;
            case "badbot":
                await MemeCommands.BadBot(arg, Client.Rest);
                break;
            case "when":
                await MemeCommands.When(arg);
                break;
            case "whycraig":
                await MemeCommands.WhyCraig(arg);
                break;
            // apple commands
            case "info":
                await AppleCommands.DeviceInfo(arg);
                break;
            case "watch":
                await AppleCommands.YesWatch(arg, Client);
                break;
            case "unwatch":
                await AppleCommands.NoWatch(arg, Client);
                break;
            case "yesthreads":
                await AppleCommands.YesThreads(arg, Client);
                break;
            case "nothreads":
                await AppleCommands.NoThreads(arg);
                break;
            case "yesforum":
                await AppleCommands.YesForum(arg, Client);
                break;
            case "noforum":
                await AppleCommands.NoForum(arg, Client);
                break;
        }
    }

    private Task ClientOnLog(LogMessage arg)
    {
        _logger.Info(arg.Message);
        if (null != arg.Exception)
        {
            Poster.StaticError(UpdatePoster, $"Bot error:\n{arg.Exception.Message}");
            _logger.Error(arg.Exception);
            _logger.Error(arg.Exception.InnerException?.StackTrace);
        }

        return Task.CompletedTask;
    }

    private async Task PauseBot(SocketSlashCommand arg, bool pause)
    {
        if (!AdminCommands.IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        PollTimer.Enabled = !pause;
        var s = PollTimer.Enabled ? "running" : "paused";
        await arg.RespondAsync($"Craig is now {s}");
    }

    private async void PollTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        using var db = new BetaContext();

        // cycle status
        var content = GetStatusContent();
        if (content == "server") content = $"Member of: {Client.Guilds.Count} Servers";

        var newStatus = $"{GetStatus()} | {content}";
        if (newStatus.StartsWith("Sleeping")) _ = Client.SetStatusAsync(UserStatus.AFK);
        else _ = Client.SetStatusAsync(UserStatus.Online);

        _logger.Info($"New status: {newStatus}");
        _ = Client.SetCustomStatusAsync(newStatus);

        // should we check for updates
        if (IsSleeping()) return;

        // set new time from config
        PollTimer.Interval = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);

        // update server count
        var channelId = ulong.Parse(db.Configs.First(c => c.Name == "StatusChannel").Value);
        var env = db.Configs.First(c => c.Name == "Environment").Value;
        var countChannel = await Client.GetChannelAsync(channelId);
        await ((IVoiceChannel)countChannel).ModifyAsync(c =>
            c.Name = $"{env} Bot Servers: {Client.Rest.GetGuildsAsync().Result.Count}");

        // get updates
        var updates = new ConcurrentBag<Update>();
        var dbUpdates = db.Updates.ToList();

        await Parallel.ForEachAsync(db.Devices.Where(d => d.Enabled), async (device, _) =>
        {
            try
            {
                var ups = await AppleService.GetUpdate(device);
                foreach (var u in ups)
                {
                    // hash == hash -> same update
                    // build, release date, category -> might be different but prevent double posting essentially the same thing
                    // check db + already found updates
                    if (!dbUpdates.Any(up => (up.Hash == u.Hash &&
                                              up.Category == u.Group) ||
                                             (up.Build == u.Build &&
                                              up.ReleaseDate == u.ReleaseDate &&
                                              up.Category == u.Group)) &&
                        !updates.Any(up => (up.Hash == u.Hash &&
                                            up.Group == u.Group) ||
                                           (up.Build == u.Build &&
                                            up.ReleaseDate == u.ReleaseDate &&
                                            up.Group == u.Group))) updates.Add(u);
                    else _logger.Info("No new update found for " + device.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                Poster.StaticError(UpdatePoster,
                    $"Error checking update for {device.FriendlyName}:\n{ex.Message}");
            }
        });

        // post updates
        foreach (var update in updates)
        {
            var category = update.Device.Category;
            var servers = db.Servers.Where(s => s.Category == category);

            _logger.Info(
                $"Update for {update.Device.FriendlyName} found. Version {update.VersionReadable} with build id {update.Build}");

            // dont post if older than 12 hours, still add to db tho
            var postOld = Convert.ToBoolean(Convert.ToInt16(db.Configs.First(c => c.Name == "PostOld").Value));

            // save update to db
            AppleService.SaveUpdate(update);

            if (!postOld && update.ReleaseDate.DayOfYear != DateTime.Today.DayOfYear)
            {
                var error =
                    $"{update.Device.FriendlyName} update {update.VersionReadable}-{update.Build} was released on {update.ReleaseDate.ToShortDateString()}. too old. not posting.";
                _logger.Info(error);
                await UpdatePoster.PostError(error);

                continue;
            }

            foreach (var server in servers)
            {
                var threads = db.Threads.Where(t => t.Category == category && t.ServerId == server.ServerId);
                var forums = db.Forums.Where(f => f.Category == category && f.ServerId == server.ServerId);
                var postedThreads = new List<string>();
                var postedForums = new List<string>();

                // post threads
                foreach (var thread in threads)
                {
                    var post = await UpdatePoster.CreateThreadAsync(thread, update);
                    if (null != post) postedThreads.Add($"https://discord.com/channels/{post.GuildId}/{post.Id}");
                }

                // post forums
                foreach (var forum in forums)
                {
                    var post = await UpdatePoster.CreateForumAsync(forum, update);
                    if (null != post) postedForums.Add($"https://discord.com/channels/{post.GuildId}/{post.Id}");
                }

                // send alert
                _ = UpdatePoster.PostUpdateAsync(server, update, postedThreads, postedForums);
            }
        }
    }

    private bool IsSleeping()
    {
        if (!PollTimer.Enabled) return false;
        var weekend = DateTime.Today.DayOfWeek == DayOfWeek.Saturday || DateTime.Today.DayOfWeek == DayOfWeek.Sunday;

        using var db = new BetaContext();

        var startTime = int.Parse(db.Configs.First(c => c.Name == "ClockInHour").Value);
        var endTime = int.Parse(db.Configs.First(c => c.Name == "ClockOutHour").Value);
        var now = DateTime.Now.Hour;

        return weekend || now < startTime || now > endTime;
    }

    private string GetStatusContent()
    {
        var statuses = new[]
        {
            "Sigma",
            "Now with 10% more AI",
            $"server",
            "Traveling on Hair Force One",
            $"Craig version: {_version}"
        };

        return statuses[new Random().Next(statuses.Length)];
    }

    private string GetStatus() => !PollTimer.Enabled ? "Paused" : IsSleeping() ? "Sleeping" : "Running";

    private IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.Guilds | GatewayIntents.GuildMessages,
            MessageCacheSize = 15
        };

        var collection = new ServiceCollection();

        collection.AddTransient<IAppleService, AppleService>();
        collection
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>();

        return collection.BuildServiceProvider();
    }
}