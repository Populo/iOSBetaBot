using Discord;
using Discord.Rest;
using iOSBot.Data;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using Timer = System.Timers.Timer;

namespace iOSBot.Bot
{
    internal class ApiSingleton
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public DiscordRestClient Bot { get; set; }

        private RestClient RestClient { get; set; }

        Timer Timer;

        private static readonly Lazy<ApiSingleton> single = new Lazy<ApiSingleton>(() => new ApiSingleton());

        public static ApiSingleton Instance => single.Value;

        private ApiSingleton()
        {
            var restOptions = new RestClientOptions("https://gdmf.apple.com/v2/assets")
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };

            RestClient = new RestClient(restOptions);

            Timer = new Timer();

#if DEBUG
            Timer.Interval = 60 * 1000; // 60 seconds
#else
            Timer.Interval = 2.5 * 1000 * 60; // 2.5 minutes
#endif
            //Timer.AutoReset = true;
            Timer.Elapsed += Timer_Elapsed;
        }

        public async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.Info("Tick");
            ConcurrentBag<Update> updates = new ConcurrentBag<Update>();
            using var db = new BetaContext();
            List<Data.Update> dbUpdates = db.Updates.ToList();
            List<Device> dbDevices = db.Devices.ToList();

            var watchedCategories = db.Servers.Select(s => s.Category).Distinct();
            List<Device> watchedDevices = dbDevices.Where(d => watchedCategories.Contains(d.Category)).ToList();

            Parallel.ForEach(watchedDevices, device =>
            {
                var u = GetUpdate(device);
                if (null != u) u.Device = device;

                if (null != u && !dbUpdates.Any(dbU => dbU.Version == u.VersionReadable && dbU.Build == u.Build)) updates.Add(u);
                else Logger.Info("No update found for " + device.FriendlyName);
            });

            foreach (var update in updates)
            {
                var category = update.Device.Category;
                var servers = db.Servers.Where(s => s.Category == category);

                foreach (var server in servers)
                {
                    await SendAlert(update, server);
                }

                var newUpdate = new Data.Update()
                {
                    Build = update.Build,
                    Category = category,
                    Guid = Guid.NewGuid(),
                    Version = update.VersionReadable
                };
                db.Updates.Add(newUpdate);

                Logger.Info($"Update for {update.Device.FriendlyName} found. Version {update.VersionReadable} with build id {update.Build}");
            }

            Timer.Interval = int.Parse(db.Configs.FirstOrDefault(c => c.Name == "Timer").Value);

            db.SaveChanges();
        }

        private async Task SendAlert(Update update, Server server)
        {
            var channel = Bot.GetChannelAsync(server.ChannelId).Result as RestTextChannel;
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
            Timer.Start();
        }

        private Update GetUpdate(Device device)
        {
            var request = new RestRequest();
            var reqBody = new AssetRequest
            {
                AssetAudience = device.AudienceId,
                ClientVersion = 2,
                BuildVersion = device.BuildId,
                HWModelStr = device.BoardId,
                ProductType = device.Product,
                ProductVersion = device.Version
            };

            reqBody.AssetType = device.Category.Contains("macOS") ? "com.apple.MobileAsset.MacSoftwareUpdate" : "com.apple.MobileAsset.SoftwareUpdate";

            request.AddJsonBody(JsonConvert.SerializeObject(reqBody));

            Logger.Trace(request.Parameters.First().Value);

            try
            {
                var response = RestClient.Post(request);
            
                var jwt = new JwtSecurityToken(response.Content);

                var claim = jwt.Claims.First(j => j.Type == "Assets").Value;
                var json = JsonConvert.DeserializeObject<AssetResponse>(claim);

                var update = new Update()
                {
                    Build = json.Build,
                    SizeBytes = json._DownloadSize,
                    ReleaseDate = DateTime.Parse(jwt.Claims.First(j => j.Type == "PostingDate").Value),
                    VersionDocId = json.SUDocumentationID,
                    Version = json.OSVersion.Replace("9.9.", ""),
                    ReleaseType = (ReleaseType)device.Type,
                    Group = device.Category
                };

                return update;
                
            } 
            catch (Exception e)
            {
                Logger.Error($"Error checking {device.Category}:\n {e.Message}");
                return null;
            }
        }
    }
}
