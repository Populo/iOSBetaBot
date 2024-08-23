using iOSBot.AdminWeb.Models;
using iOSBot.AdminWeb.Service;
using Microsoft.AspNetCore.Mvc;

namespace iOSBot.AdminWeb.Controllers
{
    public class ConfigController : Controller
    {
        public ConfigController(IDeviceService service)
        {
            _devices = service;
        }

        private IDeviceService _devices { get; set; }

        public IActionResult Index()
        {
            ViewBag.Title = "Config";

            var items = _devices.GetConfigItems();

            return View(items);
        }

        [HttpPost]
        public IActionResult Index(ConfigViewModel model)
        {
            ViewBag.Title = "Config";

            _devices.SaveConfigItems(model);

            return View();
        }
    }
}