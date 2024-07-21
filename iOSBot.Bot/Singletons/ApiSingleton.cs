using System.Collections.Concurrent;
using System.Timers;
using Discord;
using Discord.WebSocket;
using iOSBot.Data;
using iOSBot.Service;
using NLog;
using Thread = iOSBot.Data.Thread;
using Timer = System.Timers.Timer;
using Update = iOSBot.Service.Update;

namespace iOSBot.Bot.Singletons
{
    public class ApiSingleton
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();

        public DiscordSocketClient Bot { get; set; }

        private Timer _timer;

        public bool IsRunning => _timer.Enabled;
        public void StopTimer() => _timer.Stop();
        public void StartTimer() => _timer.Start();

        // probably very wrong. but yolo
        private static readonly Lazy<ApiSingleton> Single = new(() => new ApiSingleton(new AppleService()));

        public static ApiSingleton Instance => Single.Value;

        private IAppleService AppleService { get; }

        public ApiSingleton(IAppleService service)
        {
            AppleService = service;

            _timer = new Timer
            {
                Interval = 90 * 1000 // 90 seconds
            };

            _timer.Elapsed += Timer_Elapsed;
        }

        public async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Logger.Info("Tick");
            var updates = new ConcurrentBag<Update>();
            await using var db = new BetaContext();
            var dbUpdates = db.Updates.ToList();

            // check all devices for /info 
            Parallel.ForEach(db.Devices, device =>
            {
                try
                {
                    Update u = AppleService.GetUpdate(device).Result;

                    if (!dbUpdates.Any(up => up.Build == u.Build &&
                                             up.ReleaseDate == u.ReleaseDate &&
                                             up.Category == u.Group)) updates.Add(u);
                    else Logger.Info("No new update found for " + device.FriendlyName);
                }
                catch (Exception ex)
                {
                    Commands.PostError(Bot, AppleService,
                        $"Error checking update for {device.FriendlyName}:\n{ex.Message}");
                }
            });

            foreach (var update in updates)
            {
                var category = update.Device.Category;
                var servers = db.Servers.Where(s => s.Category == category);
                var threads = db.Threads.Where(t => t.Category == category);
                var forums = db.Forums.Where(f => f.Category == category);

                Logger.Info(
                    $"Update for {update.Device.FriendlyName} found. Version {update.VersionReadable} with build id {update.Build}");

                // dont post if older than 12 hours, still add to db tho
                var postOld = Convert.ToBoolean(Convert.ToInt16(db.Configs.First(c => c.Name == "PostOld").Value));

                if (postOld || update.ReleaseDate.DayOfYear == DateTime.Today.DayOfYear)
                {
                    foreach (var server in servers)
                    {
                        await SendAlert(update, server);
                    }

                    foreach (var thread in threads)
                    {
                        await CreateThread(thread, update);
                    }

                    foreach (var forum in forums)
                    {
                        await CreateForum(forum, update);
                    }
                }
                else
                {
                    var error =
                        $"{update.Device.FriendlyName} update {update.VersionReadable}-{update.Build} was released on {update.ReleaseDate.ToShortDateString()}. too old. not posting.";
                    Logger.Info(error);
                    Commands.PostError(Bot, AppleService, error);
                }

                AppleService.SaveUpdate(update);
            }

            // update server count
            ulong channelId = ulong.Parse(db.Configs.First(c => c.Name == "StatusChannel").Value);
            string env = db.Configs.First(c => c.Name == "Environment").Value;
            IChannel channel = Bot.GetChannelAsync(channelId).Result;
            await ((IVoiceChannel)channel).ModifyAsync(c =>
                c.Name = $"{env} Bot Servers: {Bot.Rest.GetGuildsAsync().Result.Count}");

            _timer.Interval = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);

            await db.SaveChangesAsync();
        }

        private async Task CreateForum(Forum forum, Update update)
        {
            var dForum = (await Bot.GetChannelAsync(forum.ChannelId)) as IForumChannel;
            Logger.Info($"Creating post in {dForum.Name} for {update.VersionReadable}");
            var embed = new EmbedBuilder()
            {
                Color = update.Device.Color,
                Title = update.VersionReadable,
            };
            embed.AddField(name: "Build", value: update.Build)
                .AddField(name: "Size", value: update.Size)
                .AddField(name: "Release Date", value: update.ReleaseDate.ToShortDateString())
                .AddField(name: "Changelog", value: update.Device.Changelog);

            var imagePath = GetImagePath(update.Device.Category);
            if (!string.IsNullOrEmpty(imagePath))
            {
                embed.ThumbnailUrl = imagePath;
            }

            await dForum.CreatePostAsync(title: $"{update.VersionReadable} Discussion",
                text: $"Discuss the release of {update.VersionReadable} here.",
                embed: embed.Build(),
                archiveDuration: ThreadArchiveDuration.OneWeek);
            Logger.Info("Forum post created");
        }

        private async Task CreateThread(Thread thread, Update update)
        {
            var channel = (ITextChannel)Bot.GetChannelAsync(thread.ChannelId).Result;
            Logger.Info($"Creating thread in {channel} for {update.VersionReadable}");

            await channel.CreateThreadAsync($"{update.VersionReadable} Release Thread");
            Logger.Info("Thread Created.");
        }

        public async Task SendAlert(Update update, Server server)
        {
            var channel = (ITextChannel)Bot.GetChannelAsync(server.ChannelId).Result;
            if (null == channel)
            {
                Logger.Warn($"Channel with id {server.ChannelId} doesnt exist. Removing");
                AppleService.DeleteServer(server);

                return;
            }

            var mention = server.TagId != "" ? $"<@&{server.TagId}>" : "";

            var embed = new EmbedBuilder
            {
                Color = new Color(update.Device.Color),
                Title = $"New {update.Device.FriendlyName} Release!",
                Timestamp = DateTime.Now,
            };
            embed.AddField(name: "Version", value: update.VersionReadable)
                .AddField(name: "Build", value: update.Build)
                .AddField(name: "Size", value: update.Size);

            var imagePath = GetImagePath(update.Device.Category);
            if (!string.IsNullOrEmpty(imagePath))
            {
                embed.ThumbnailUrl = imagePath;
            }

            if (!string.IsNullOrEmpty(update.Device.Changelog))
            {
                embed.Url = update.Device.Changelog;
            }

            Logger.Info($"Posting {update.VersionReadable} to {channel.Name}");
            try
            {
                await channel.SendMessageAsync(text: mention, embed: embed.Build());
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Commands.PostError(Bot, AppleService, $"Error posting to {channel.Name}. {e.Message}");
            }
        }

        private string GetImagePath(string category)
        {
            if (category.Contains("ios", StringComparison.CurrentCultureIgnoreCase))
                return
                    "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/iphone.png";
            else if (category.Contains("mac", StringComparison.CurrentCultureIgnoreCase))
                return
                    "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/mac.png";
            else if (category.Contains("tv", StringComparison.CurrentCultureIgnoreCase))
                return
                    "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/tv.png";
            else if (category.Contains("watch", StringComparison.CurrentCultureIgnoreCase))
                return
                    "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/watch.png";
            else if (category.Contains("vision", StringComparison.CurrentCultureIgnoreCase))
                return
                    "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/vision.png";

            return string.Empty;
        }

        public void Start()
        {
            _timer.Start();
        }
    }
}