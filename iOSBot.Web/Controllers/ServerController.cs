using Microsoft.AspNetCore.Mvc;

namespace iOSBot.Web.Controllers;

public class ServerController : Controller
{
    public ActionResult Index()
    {
        return View();
    }
}