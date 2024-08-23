using Microsoft.AspNetCore.Mvc;

namespace iOSBot.AdminWeb.Controllers;

public class ServerController : Controller
{
    public ActionResult Index()
    {
        return View();
    }
}