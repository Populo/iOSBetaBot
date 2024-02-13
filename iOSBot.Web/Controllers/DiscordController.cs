using iOSBot.Service;
using Microsoft.AspNetCore.Mvc;

namespace iOSBot.Web.Controllers;

public class DiscordController : Controller
{
    private IDiscordService _discord;

    public DiscordController(IDiscordService discord)
    {
        _discord = discord;
    }

    // GET
    public IActionResult Index()
    {
        return View();
    }

    public DiscordServer GetServer(ulong id)
    {
        return _discord.GetServerAndChannels(id);
    }
}