﻿using System.IdentityModel.Tokens.Jwt;
using System.Runtime.ExceptionServices;
using iOSBot.Data;
using Newtonsoft.Json;
using NLog;
using RestSharp;

namespace iOSBot.Service
{
    public interface IAppleService
    {
        public Task<Update> GetUpdate(Device device);
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
        Logger _logger = LogManager.GetCurrentClassLogger();

        private RestClient RestClient { get; set; }

        public AppleService()
        {
            var restOptions = new RestClientOptions("https://gdmf.apple.com/v2/assets")
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };

            RestClient = new RestClient(restOptions);
        }

        public async Task<Update> GetUpdate(Device device)
        {
            _logger.Info($"Getting updates for {device.FriendlyName}");
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

            request.AddJsonBody(JsonConvert.SerializeObject(reqBody));

            try
            {
                var response = await RestClient.PostAsync(request);

                var jwt = new JwtSecurityToken(response.Content);

                var claim = jwt.Claims.First(j => j.Type == "Assets").Value;
                var json = JsonConvert.DeserializeObject<AssetResponse>(claim);

                var update = new Update
                {
                    Build = json.Build,
                    SizeBytes = json._DownloadSize,
                    ReleaseDate = DateTime.Parse(jwt.Claims.First(j => j.Type == "PostingDate").Value),
                    VersionDocId = json.SUDocumentationID,
                    Version = json.OSVersion.Replace("9.9.", ""),
                    ReleaseType = device.Type,
                    Group = device.Category,
                    Device = device
                };

                return update;

            }
            catch (Exception e)
            {
                string error = $"Error checking {device.Category}:\n {e.Message}";
                _logger.Error(error);
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        public void SaveUpdate(Update update)
        {
            using var db = new BetaContext();

            var newUpdate = new Data.Update
            {
                Build = update.Build,
                Category = update.Device.Category,
                Guid = Guid.NewGuid(),
                Version = update.VersionReadable
            };
            db.Updates.Add(newUpdate);

            db.SaveChangesAsync();
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
            _logger.Info("Getting all devices");
            using var db = new BetaContext();
            return db.Devices.ToList();
        }

        public void AddDevice(Device device)
        {
            using var db = new BetaContext();
            device.Changelog ??= "";

            db.Devices.Add(device);
            _logger.Info($"Adding device to find updates for {device.FriendlyName}");

            db.SaveChangesAsync();
        }

        public void RemoveDevice(Device device)
        {
            _logger.Info($"Deleting {device.FriendlyName}");
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

        public Dictionary<string,string> GetConfigItems()
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