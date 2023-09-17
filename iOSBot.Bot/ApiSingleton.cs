using Discord;
using Discord.Rest;
using iOSBot.Data;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.ComTypes;
using iOSBot.Service;
using Timer = System.Timers.Timer;

namespace iOSBot.Bot
{
    public class ApiSingleton
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public DiscordRestClient? Bot { get; set; }

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
            var dbDevices = db.Devices.ToList();

            var watchedCategories = db.Servers.Select(s => s.Category).Distinct();
            var watchedDevices = dbDevices.Where(d => watchedCategories.Contains(d.Category)).ToList();

            Parallel.ForEach(watchedDevices, device =>
            {
                try
                {
                    Service.Update u = AppleService.GetUpdate(device).Result;
                    
                    if (!dbUpdates.Any(up => up.Version == u.VersionReadable && up.Build == u.Build)) updates.Add(u);
                    else Logger.Info("No new update found for " + device.FriendlyName);
                }
                catch (Exception ex)
                {
                    PostError($"Error checking update for {device.FriendlyName}:\n{ex.Message}");
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
                    PostError(error);
                }
                
                AppleService.SaveUpdate(update);
            }

            _timer.Interval = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);

            await db.SaveChangesAsync();
        }

        private async Task SendAlert(Service.Update update, Server server)
        {
            var channel = (await Bot!.GetChannelAsync(server.ChannelId)) as RestTextChannel;
            if (null == channel)
            {
                Logger.Warn($"Channel with id {server.ChannelId} doesnt exist. Removing");
                AppleService.DeleteServer(server);

                return;
            }
            var role = server.TagId != "" ? Bot.GetGuildAsync(server.ServerId).Result.GetRole(ulong.Parse(server.TagId)).Mention : "";

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
            await channel.SendMessageAsync(text: role, embed: embed.Build());
        }

        public void Start()
        {
            _timer.Start();
        }

        public async void PostError(string? message)
        {
            try
            {
                await using var db = new BetaContext();

                foreach (var s in db.ErrorServers)
                {
                    var server = (RestTextChannel)Bot.GetChannelAsync(s.ChannelId).Result;
                    if (null == server)
                    {
                        AppleService.DeleteErrorServer(s, db);
                        continue;
                    }
                    if (message != "Server requested a reconnect" &&
                        message != "WebSocket connection was closed")
                    {
                        await server.SendMessageAsync(message);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Environment.Exit(1);
            }
        }
    }
}
