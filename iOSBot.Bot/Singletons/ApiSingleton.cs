using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using iOSBot.Data;
using iOSBot.Service;
using Timer = System.Timers.Timer;

namespace iOSBot.Bot.Singletons
{
    public class ApiSingleton
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public DiscordSocketClient Bot { get; set; }

        private Timer _timer;

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

        public async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.Info("Tick");
            var updates = new ConcurrentBag<Service.Update>();
            await using var db = new BetaContext();
            var dbUpdates = db.Updates.ToList();

            // check all devices for /info 
            Parallel.ForEach(db.Devices, device =>
            {
                try
                {
                    Service.Update u = AppleService.GetUpdate(device).Result;
                    
                    if (!dbUpdates.Any(up => up.Build == u.Build &&
                                             up.ReleaseDate == u.ReleaseDate &&
                                             up.Category == u.Group)) updates.Add(u);
                    else Logger.Info("No new update found for " + device.FriendlyName);
                }
                catch (Exception ex)
                {
                    Commands.PostError(Bot, AppleService, $"Error checking update for {device.FriendlyName}:\n{ex.Message}");
                }
            });

            foreach (var update in updates)
            {
                var category = update.Device.Category;
                var servers = db.Servers.Where(s => s.Category == category);

                Logger.Info($"Update for {update.Device.FriendlyName} found. Version {update.VersionReadable} with build id {update.Build}");

                // dont post if older than 12 hours, still add to db tho
                var postOld = Convert.ToBoolean(Convert.ToInt16(db.Configs.First(c => c.Name == "PostOld").Value));
                
                if (postOld || update.ReleaseDate.DayOfYear == DateTime.Today.DayOfYear)
                {    
                    foreach (var server in servers)
                    {
                        await SendAlert(update, server);
                    }
                } 
                else
                {
                    var error = $"{update.Device.FriendlyName} update {update.VersionReadable}-{update.Build} was released on {update.ReleaseDate.ToShortDateString()}. too old. not posting.";
                    Logger.Info(error);
                    Commands.PostError(Bot, AppleService, error);
                }
                
                AppleService.SaveUpdate(update);
            }
            
            // update server count
            ulong channelId = ulong.Parse(db.Configs.First(c => c.Name == "StatusChannel").Value);
            string env = db.Configs.First(c => c.Name == "Environment").Value;
            IChannel channel = Bot.GetChannelAsync(channelId).Result;
            await ((IVoiceChannel)channel).ModifyAsync(c => c.Name = $"{env} Bot Servers: {Bot.Rest.GetGuildsAsync().Result.Count}");

            _timer.Interval = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);

            await db.SaveChangesAsync();
        }

        private async Task SendAlert(Service.Update update, Server server)
        {
            var channel = (ITextChannel) Bot.GetChannelAsync(server.ChannelId).Result;
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

        public void Start()
        {
            _timer.Start();
        }
    }
}
