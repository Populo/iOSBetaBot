using iOSBot.AdminWeb.Models;
using iOSBot.Data;
using iOSBot.Service;
using Mapster;
using Update = iOSBot.Service.Update;

namespace iOSBot.AdminWeb.Service
{
    public interface IDeviceService
    {
        public bool ModifyDevice(DeviceViewModel device);
        public bool DeleteDevice(string audienceId);
        public bool CreateDevice(DeviceViewModel device);
        public List<DeviceViewModel> GetAllDevices();
        public DeviceViewModel GetDeviceByAudience(string audienceId);

        public Task<List<Update>> TestDevice(string audienceId, string product, string boardId, string fwVersion,
            string fwBuild, string assetType, string feed);

        public ConfigViewModel GetConfigItems();
        public void SaveConfigItems(ConfigViewModel items);
    }

    public class DeviceService : IDeviceService
    {
        private IAppleService _appleService;

        public DeviceService(IAppleService appleService)
        {
            _appleService = appleService;
        }

        public bool ModifyDevice(DeviceViewModel device)
        {
            var dbDevice = _appleService.GetDevice(device.AudienceId);
            if (null == dbDevice) return false;

            _appleService.ModifyDevice(device.Adapt(new Device()));
            return true;
        }

        public bool DeleteDevice(string audienceId)
        {
            var device = _appleService.GetDevice(audienceId);
            if (null == device) return false;

            _appleService.RemoveDevice(device);

            return true;
        }

        public bool CreateDevice(DeviceViewModel device)
        {
            if (null != _appleService.GetDevice(device.AudienceId)) return false;

            _appleService.AddDevice(device.Adapt(new Device()));
            return true;
        }

        public List<DeviceViewModel> GetAllDevices()
        {
            var dbDevices = _appleService.GetAllDevices();

            return dbDevices.Adapt(new List<DeviceViewModel>());
        }

        public DeviceViewModel GetDeviceByAudience(string audienceId)
        {
            var device = _appleService.GetDevice(audienceId);

            return device.Adapt(new DeviceViewModel());
        }

        public async Task<List<Update>> TestDevice(string audienceId, string product, string boardId, string fwVersion,
            string fwBuild, string assetType, string feed)
        {
            List<Update> u;
            try
            {
                u = await _appleService.GetUpdate(new Device()
                {
                    AssetType = assetType,
                    AudienceId = audienceId,
                    FriendlyName = $"Test device from web ui. {product} on {fwVersion}",
                    BoardId = boardId,
                    BuildId = fwBuild,
                    Product = product,
                    Version = fwVersion,
                    Type = feed
                });
            }
            catch (Exception ex)
            {
                u = new List<Update>()
                {
                    new Update()
                    {
                        Version = "bad",
                        Build = ex.StackTrace,
                        Group = ex.Message
                    }
                };
            }

            return u;
        }

        public ConfigViewModel GetConfigItems()
        {
            var model = _appleService.GetConfigItems();

            return new ConfigViewModel()
            {
                Delay = int.Parse(model["Timer"]),
                PostAnyway = model["PostOld"] == "1",
                ManifestGif = model["ManifestGif"]
            };
        }

        public void SaveConfigItems(ConfigViewModel model)
        {
            _appleService.SaveConfigItems(new List<Config>
            {
                new()
                {
                    Value = model.Delay.ToString(),
                    Name = "Timer"
                },
                new()
                {
                    Value = model.PostAnyway ? "1" : "0",
                    Name = "PostOld"
                },
                new()
                {
                    Value = model.ManifestGif,
                    Name = "ManifestGif"
                }
            });
        }
    }
}