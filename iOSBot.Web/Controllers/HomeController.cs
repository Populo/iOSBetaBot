using iOSBot.Web.Models;
using iOSBot.Web.Service;
using Microsoft.AspNetCore.Mvc;

namespace iOSBot.Web.Controllers
{
    public class HomeController : Controller
    {
        private IDeviceService _devices { get; }

        public HomeController(IDeviceService service)
        {
            _devices = service;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Devices";
            return View();
        }

        [HttpPost]
        public IActionResult Index(DeviceViewModel device)
        {
            ViewBag.Title = "Devices";

            if (null != _devices.GetDeviceByAudience(device.AudienceId))
            {
                _devices.ModifyDevice(device);
            }
            else
            {
                _devices.CreateDevice(device);
            }

            return View();
        }

        public IActionResult DevicePartial(string audienceId)
        {
            var device = _devices.GetDeviceByAudience(audienceId);
            return PartialView("_Device", device);
        }

        public IActionResult ListDevicePartial()
        {
            List<DeviceViewModel> devices = _devices.GetAllDevices();
            return PartialView("_ExistingDevices", devices);
        }

        public async Task<IActionResult> TestDevicePartial(string audienceId, string product, string boardId,
            string fwVersion,
            string fwBuild, string assetType, string feed)
        {
            var update = await _devices.TestDevice(audienceId, product, boardId, fwVersion, fwBuild, assetType, feed);

            return PartialView("_TestDevice", update);
        }

        [HttpPost]
        public IActionResult DeleteDevice(string audienceId)
        {
            _devices.DeleteDevice(audienceId);
            return RedirectToAction("Index");
        }
    }
}