﻿using System.Collections.Concurrent;
using System.Timers;
using Bluesky.Net;
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
    private Version _version = new(2024, 11, 14, 1);

    public Craig()
    {
        var serviceProvider = CreateProvider();

        Client = serviceProvider.GetRequiredService<DiscordSocketClient>()
                 ?? throw new Exception("Cannot get client from factory");
        AppleService = serviceProvider.GetRequiredService<IAppleService>();
        BlueSkyService = new BlueSkyService(serviceProvider.GetRequiredService<IBlueskyApi>());

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
    private BlueSkyService BlueSkyService { get; set; }
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
            //_ = BlueSkyService.PostUpdate("testing 123");
            return AdminCommands.UpdateCommands(Client, null, true);
        };
        Client.Log += ClientOnLog;
        Client.SlashCommandExecuted += ClientOnSlashCommandExecuted;
        Client.MessageReceived += ClientOnMessageReceived;

        await Client.LoginAsync(TokenType.Bot, args[0]);
        await Client.SetCustomStatusAsync(Status);

        await Client.StartAsync();
        PollTimer.Start();

        _logger.Info("Started");
        await Task.Delay(-1);
    }

    private async Task ClientOnMessageReceived(SocketMessage arg)
    {
        if (arg.Author.IsBot) return;

        using var db = new BetaContext();
        var dmForum =
            await Client.GetChannelAsync(ulong.Parse(db.Configs.First(c => c.Name == "DMForum").Value)) as
                SocketForumChannel
            ?? throw new Exception("Cannot get DM Forum from Id provided from database");

        if (arg.Channel is IThreadChannel threadChannel)
        {
            var restThread = await Client.Rest.GetChannelAsync(threadChannel.Id) as RestThreadChannel
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
            var user = await Client.Rest.GetUserAsync(ulong.Parse(userId));
            await user.SendMessageAsync($"{arg.Content}\n-@{arg.Author.Username}");
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
                _ = AdminCommands.YesErrors(arg, Client);
                break;
            case "noerrors":
                _ = AdminCommands.NoErrors(arg, Client);
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
                _ = AdminCommands.UpdateCommands(Client, arg);
                break;
            case "servers":
                _ = AdminCommands.GetServers(arg, Client);
                break;
            case "start":
                _ = PauseBot(arg, false);
                break;
            case "stop":
                _ = PauseBot(arg, true);
                break;
            case "fake":
                _ = AdminCommands.FakeUpdate(arg, UpdatePoster);
                break;
            case "toggle":
                _ = AdminCommands.ToggleDevice(arg);
                break;
            // meme commands
            case "manifest":
                _ = MemeCommands.Manifest(arg);
                break;
            case "goodbot":
                _ = MemeCommands.GoodBot(arg, Client);
                break;
            case "badbot":
                _ = MemeCommands.BadBot(arg, Client);
                break;
            case "when":
                _ = MemeCommands.When(arg);
                break;
            case "whycraig":
                _ = MemeCommands.WhyCraig(arg);
                break;
            // apple commands
            case "info":
                _ = AppleCommands.DeviceInfo(arg);
                break;
            case "watch":
                _ = AppleCommands.YesWatch(arg, Client);
                break;
            case "unwatch":
                _ = AppleCommands.NoWatch(arg, Client);
                break;
            case "yesthreads":
                _ = AppleCommands.YesThreads(arg, Client);
                break;
            case "nothreads":
                _ = AppleCommands.NoThreads(arg);
                break;
            case "yesforum":
                _ = AppleCommands.YesForum(arg, Client);
                break;
            case "noforum":
                _ = AppleCommands.NoForum(arg, Client);
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

        // set new time from config
        PollTimer.Interval = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);

        // update server count
        var channelId = ulong.Parse(db.Configs.First(c => c.Name == "StatusChannel").Value);
        var env = db.Configs.First(c => c.Name == "Environment").Value;
        var countChannel = await Client.GetChannelAsync(channelId);
        await ((IVoiceChannel)countChannel).ModifyAsync(c =>
            c.Name = $"{env} Bot Servers: {Client.Rest.GetGuildsAsync().Result.Count}");

        // should we check for updates
        if (IsSleeping() && null != sender) return;

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
                    /*
                     * cases:
                     * 1) new update
                     *      -> post as normal
                     * 2) nothing new
                     *      -> do nothing
                     * 3) re-release with same build
                     *      -> post
                     * 4) new release date but same hash
                     *      -> dont post
                     */

                    var post = false;
                    var existingDb = dbUpdates.Where(d => d.Category == u.Group && d.Build == u.Build)
                        .ToList();

                    // we've seen this build before
                    if (existingDb.Any())
                    {
                        // new hash of same update
                        if (!existingDb.Any(d => d.Hash == u.Hash))
                        {
                            // not already going to post this build found just now
                            if (!updates.Any(d => d.Group == u.Group && d.Build == u.Build))
                            {
                                post = true;
                            }
                        }
                    }
                    else
                    {
                        // not saved in db, but have we seen it yet this loop?
                        if (updates.Any(d => d.Group == u.Group && d.Build == u.Build))
                        {
                            // yes, save anyway even though we arent posting
                            AppleService.SaveUpdate(u);
                        }
                        else
                        {
                            // no, post it (and save later)
                            post = true;
                        }
                    }

                    if (post) updates.Add(u);
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

            // post to bluesky
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Release")
            {
                _ = BlueSkyService.PostUpdate(
                    $"New update found.\n\n\nTrack: {update.Device.FriendlyName}\nVersion: {update.VersionReadable}\nBuild: {update.Build}");
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
            $"Craig version: {_version}",
            "DM me for help :)"
        };

        return statuses[new Random().Next(statuses.Length)];
    }

    private string GetStatus() => !PollTimer.Enabled ? "Paused" : IsSleeping() ? "Sleeping" : "Running";

    private IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent,
            MessageCacheSize = 15
        };

        var collection = new ServiceCollection();

        collection.AddBluesky();
        collection.AddTransient<IAppleService, AppleService>();
        collection
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>();

        return collection.BuildServiceProvider();
    }
}