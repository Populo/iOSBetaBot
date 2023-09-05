using iOSBot.Web.Models;
using iOSBot.Web.Service;
using Microsoft.AspNetCore.Mvc;

namespace iOSBot.Web.Controllers
{
    public class ConfigController : Controller
    {
        private IDeviceService _devices { get; set; }

        public ConfigController(IDeviceService service)
        {
            _devices = service;
        }

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
