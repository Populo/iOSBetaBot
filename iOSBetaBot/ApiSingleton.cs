using Discord;
using Discord.WebSocket;
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

        public DiscordSocketClient Bot { get; set; }

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
            Timer.Interval = 15 * 1000; // 60 seconds
#else
            Timer.Interval = 5 * 1000 * 60; // 5 minutes
#endif
            //Timer.AutoReset = true;
            Timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.Info("Tick");
            ConcurrentBag<Update> updates = new ConcurrentBag<Update>();
            using var db = new BetaContext();
            List<Data.Update> dbUpdates = db.Updates.ToList();

            var watchedCategories = db.Servers.Select(s => s.Category).Distinct();
            List<Device> watchedDevices = Helpers.Devices.Where(d => watchedCategories.Contains(d.Category)).ToList();

            Parallel.ForEach(watchedDevices, device =>
            {
                var u = GetUpdate(device.Audience, device.BuildId, device.BoardId, device.Product, device.Version, device.Type, device.Category);
                u.Device = device;

                if (!dbUpdates.Any(dbU => dbU.Version == u.VersionReadable && dbU.Build == u.Build)) updates.Add(u);
                else Logger.Info("No update found for " + device.FriendlyName);
            });

            foreach (var update in updates)
            {
                var category = update.Device.Category;
                var servers = db.Servers.Where(s => s.Category == category);

                foreach (var server in servers)
                {
                    await SendAlert(update, Bot.GetChannel(server.ChannelId) as SocketTextChannel, update.Device);
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

            db.SaveChanges();
        }

        private async Task SendAlert(Update update, SocketTextChannel? channel, Device device)
        {
            var categoryInfo = Helpers.CategoryColors.FirstOrDefault(c => c.Category == device.Category);

            var embed = new EmbedBuilder
            {
                Color = categoryInfo.Color,
                Title = $"New {device.FriendlyName} Release!",
                Timestamp = DateTime.Now,
            };
            embed.AddField(name: "Version", value: update.VersionReadable)
                .AddField(name: "Build", value: update.Build)
                .AddField(name: "Size", value: update.Size);

            if (!device.Category.Contains("audioOS"))
            {
                categoryInfo.Version = update.ChangelogVersion;
                categoryInfo.Category = device.Changelog.ToLower();
                embed.Url = categoryInfo.ChangeUrl;
            }

            await channel.SendMessageAsync(embed: embed.Build());
        }

        public void Start()
        {
            Timer.Start();
        }

        private Update GetUpdate(string audience, string build, string model, string product, string version, ReleaseType releaseType, string category)
        {
            var request = new RestRequest();
            var reqBody = new AssetRequest
            {
                AssetAudience = audience,
                ClientVersion = 2,
                BuildVersion = build,
                HWModelStr = model,
                ProductType = product,
                ProductVersion = version
            };

            reqBody.AssetType = category.Contains("macOS") ? "com.apple.MobileAsset.MacSoftwareUpdate" : "com.apple.MobileAsset.SoftwareUpdate";

            request.AddJsonBody(JsonConvert.SerializeObject(reqBody));

            Logger.Trace(request.Parameters.First().Value);
            
            var response = RestClient.Post(request);

            try
            {
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
                    ReleaseType = releaseType,
                    Group = category
                };

                return update;
                
            } 
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
