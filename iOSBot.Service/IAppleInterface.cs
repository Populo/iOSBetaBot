using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using iOSBot.Data;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Newtonsoft.Json;
using RestSharp;

namespace iOSBot.Service
{
    public interface IAppleService
    {
        public Task<List<Update>> GetUpdate(Device device);
        public bool ShouldPost(Update update, ref ConcurrentBag<Update> updates);
        public void SaveUpdate(Update update);
        public void DeleteServer(Server server, BetaContext? db = null);
        public void DeleteErrorServer(ErrorServer server, BetaContext? db = null);
        public Device? GetDevice(string audienceId, BetaContext? db = null);
        public List<Device> GetAllDevices();
        public void AddDevice(Device device);
        public void RemoveDevice(Device device);
        public void ModifyDevice(Device device);
        public Dictionary<string, string> GetConfigItems();
        public void SaveConfigItems(List<Config> config);
    }

    public class AppleService : IAppleService
    {
        private ILogger<AppleService> _logger;

        public AppleService(ILogger<AppleService> logger)
        {
            var restOptions = new RestClientOptions("https://gdmf.apple.com/v2/assets")
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };

            RestClient = new RestClient(restOptions);
            _logger = logger;
        }

        private RestClient RestClient { get; set; }

        public async Task<List<Update>> GetUpdate(Device device)
        {
            _logger.LogInformation($"Getting updates for {device.FriendlyName}");
            var request = new RestRequest();
            var reqBody = new AssetRequest
            {
                AssetAudience = device.AudienceId,
                ClientVersion = 2,
                BuildVersion = device.BuildId,
                HWModelStr = device.BoardId,
                ProductType = device.Product,
                ProductVersion = device.Version,
                AssetType = device.AssetType
            };

            var updates = new List<Update>();

            request.AddJsonBody(JsonConvert.SerializeObject(reqBody));
            RestResponse response;
            try
            {
                response = await RestClient.PostAsync(request);

                var jwt = new JsonWebToken(response.Content);

                // no assets present
                if (!jwt.Claims.Any(c => c.Type == "Assets"))
                {
                    throw new Exception($"No firmware is being signed for {device.FriendlyName}");
                }

                var assets = jwt.Claims
                    .Where(j => j.Type == "Assets");

                foreach (var asset in assets)
                {
                    var json = JsonConvert.DeserializeObject<AssetResponse>(asset.Value);
                    string hashString = json.ArchiveID;

                    // try
                    // {
                    //     var hashAlgorithm = new Sha3Digest(512);
                    //
                    //     byte[] input = Encoding.ASCII.GetBytes(json._AssetReceipt.AssetSignature);
                    //
                    //     hashAlgorithm.BlockUpdate(input, 0, input.Length);
                    //
                    //     byte[] result = new byte[64]; // 512 / 8 = 64
                    //     hashAlgorithm.DoFinal(result, 0);
                    //
                    //     hashString = BitConverter.ToString(result);
                    //     hashString = hashString.Replace("-", "").ToLowerInvariant();
                    // }
                    // catch (Exception ex)
                    // {
                    //     _logger.LogError(ex, "Cannot hash signature. Falling back to ArchiveID");
                    // }

                    var update = new Update
                    {
                        Build = json.Build,
                        SizeBytes = json._DownloadSize,
                        ReleaseDate = DateTime.Parse(jwt.Claims.First(j => j.Type == "PostingDate").Value),
                        VersionDocId = json.SUDocumentationID,
                        Version = json.OSVersion.Replace("9.9.", ""),
                        ReleaseType = device.Type,
                        Group = device.Category,
                        Device = device,
                        Hash = hashString
                    };

                    /*
                     * 3 cases:
                     *
                     * 1. completely new version
                     *  - proceed as normal, no revision
                     * 2. new build of existing version
                     *  - revision + 1
                     * 3. same build of same version
                     *  - do nothing
                     */

                    using var db = new BetaContext();
                    var dbUpdates = db.Updates
                        .Where(u => u.Version.Contains(update.VersionReadable) &&
                                    u.Category == update.Group)
                        .OrderByDescending(u => u.ReleaseDate);

                    // case 1 || 3, short circuit to prevent any kind of npe
                    // first update of this version (17.0 beta 8, 17.0 GM, etc)
                    if (!dbUpdates.Any() ||
                        dbUpdates.Any(u => u.Build == update.Build &&
                                           update.ReleaseDate == u.ReleaseDate))
                    {
                        updates.Add(update);
                        continue;
                    }

                    // case 2
                    // attempt to prevent double counting releases in the situation where it detects
                    // update but then immediately after detects the old version because of apple server stuff 
                    if (!dbUpdates.Any(u => u.Hash == update.Hash))
                    {
                        update.Revision = dbUpdates.Count();
                    }

                    updates.Add(update);
                }
            }
            catch (Exception e)
            {
                string error = $"Error checking {device.Category}:\n {e.Message}";
                _logger.LogError(error);
                ExceptionDispatchInfo.Capture(e).Throw();
                throw;
            }

            return updates;
        }

        public bool ShouldPost(Update update, ref ConcurrentBag<Update> updates)
        {
            using var db = new BetaContext();
            var dbUpdates = db.Updates.ToList();
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
            var existingDb = dbUpdates.Where(d => d.Category == update.Group && d.Build == update.Build)
                .ToList();

            // we've seen this build before
            if (existingDb.Any())
            {
                // new hash of same update
                if (!existingDb.Any(d => d.Hash == update.Hash))
                {
                    // not already going to post this build found just now
                    if (!updates.Any(d => d.Group == update.Group && d.Build == update.Build))
                    {
                        post = true;
                    }
                }
            }
            else
            {
                // not saved in db, but have we seen it yet this loop?
                if (updates.Any(d => d.Group == update.Group && d.Build == update.Build))
                {
                    // yes, save anyway even though we arent posting
                    SaveUpdate(update);
                }
                else
                {
                    // no, post it (and save later)
                    post = true;
                }
            }

            return post;
        }

        public void SaveUpdate(Update update)
        {
            using var db = new BetaContext();

            var newUpdate = new Data.Update
            {
                Build = update.Build,
                Category = update.Device.Category,
                Guid = Guid.NewGuid(),
                Version = update.VersionReadable,
                ReleaseDate = update.ReleaseDate,
                Hash = update.Hash,
            };
            db.Updates.Add(newUpdate);

            db.SaveChangesAsync().Wait();
        }

        public void DeleteServer(Server server, BetaContext? db)
        {
            var gc = false;
            if (null == db)
            {
                gc = true;
                db = new BetaContext();
            }

            db.Servers.Remove(server);
            db.SaveChangesAsync();

            if (gc) db.Dispose();
        }

        public void DeleteErrorServer(ErrorServer server, BetaContext? db)
        {
            var gc = false;
            if (null == db)
            {
                gc = true;
                db = new BetaContext();
            }

            db.ErrorServers.Remove(server);
            db.SaveChangesAsync();

            if (gc) db.Dispose();
        }

        public Device? GetDevice(string audienceId, BetaContext? db)
        {
            var gc = false;
            if (null == db)
            {
                db = new BetaContext();
                gc = true;
            }

            var device = db.Devices.FirstOrDefault(d => d.AudienceId == audienceId);

            if (gc) db.Dispose();

            return device;
        }

        public List<Device> GetAllDevices()
        {
            _logger.LogInformation("Getting all devices");
            using var db = new BetaContext();
            return db.Devices.ToList();
        }

        public void AddDevice(Device device)
        {
            using var db = new BetaContext();
            device.Changelog ??= "";

            db.Devices.Add(device);
            _logger.LogInformation($"Adding device to find updates for {device.FriendlyName}");

            db.SaveChangesAsync();
        }

        public void RemoveDevice(Device device)
        {
            _logger.LogInformation($"Deleting {device.FriendlyName}");
            using var db = new BetaContext();
            db.Devices.Remove(device);
            db.SaveChangesAsync();
        }

        public void ModifyDevice(Device device)
        {
            using var db = new BetaContext();
            var dbDevice = GetDevice(device.AudienceId, db);

            device.Changelog ??= "";

            dbDevice.Changelog = device.Changelog;
            dbDevice.FriendlyName = device.FriendlyName;
            dbDevice.Name = device.Name;
            dbDevice.BoardId = device.BoardId;
            dbDevice.Category = device.Category;
            dbDevice.BuildId = device.BuildId;
            dbDevice.Version = device.Version;
            dbDevice.Type = device.Type;
            dbDevice.Color = device.Color;
            dbDevice.Product = device.Product;
            dbDevice.AssetType = device.AssetType;

            db.SaveChanges();
        }

        public Dictionary<string, string> GetConfigItems()
        {
            using var db = new BetaContext();
            var configs = db.Configs;

            return configs.ToDictionary(config => config.Name, config => config.Value);
        }

        public void SaveConfigItems(List<Config> configs)
        {
            using var db = new BetaContext();

            foreach (var config in configs)
            {
                db.Configs.FirstOrDefault(c => c.Name == config.Name).Value = config.Value;
            }

            db.SaveChanges();
        }
    }
}