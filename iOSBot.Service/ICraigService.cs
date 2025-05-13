using System.Collections.Concurrent;
using iOSBot.Data;
using Microsoft.Extensions.Logging;

namespace iOSBot.Service;

public interface ICraigService
{
    int GetSecondsDelay();
    bool IsSleeping();
    bool TogglePause(bool pause);
    bool IsPaused();
    string GetStatusContent();
    string GetOperationStatus();
    Version GetVersion();
    IEnumerable<ErrorServer> GetErrorServers();
    Task CheckForUpdates();
    Task PostUpdateNotification(Server server, Update update, bool skipExtras = false);
    string GetTier();
}

public class CraigService(
    ILogger<CraigService> logger,
    IAppleService appleService)
    : ICraigService
{
    public IDiscordService DiscordService { get; set; }

    public int GetSecondsDelay()
    {
        using var db = new BetaContext();
        var delay = int.Parse(db.Configs.First(c => c.Name == "Delay").Value);
        return delay;
    }

    public bool IsSleeping()
    {
        if (IsPaused()) return false;
        var weekend = DateTime.Today.DayOfWeek == DayOfWeek.Saturday || DateTime.Today.DayOfWeek == DayOfWeek.Sunday;

        using var db = new BetaContext();

        var startTime = int.Parse(db.Configs.First(c => c.Name == "ClockInHour").Value);
        var endTime = int.Parse(db.Configs.First(c => c.Name == "ClockOutHour").Value);
        var now = DateTime.Now.Hour;

        return weekend || now < startTime || now > endTime;
    }

    public bool TogglePause(bool pause)
    {
        using var db = new BetaContext();
        db.Configs.First(c => c.Name == "Paused").Value = pause.ToString();
        db.SaveChanges();

        return IsPaused();
    }

    public bool IsPaused()
    {
        using var db = new BetaContext();
        return bool.Parse(db.Configs.First(c => c.Name == "Paused").Value);
    }

    public string GetStatusContent()
    {
        var statuses = new[]
        {
            "Now with 10% more AI",
            $"server",
            "Traveling on Hair Force One",
            $"Craig version: {GetVersion()}",
            "DM me for help :)",
            "/craiginfo",
            "\ud83c\udff3\ufe0f\u200d\u26a7\ufe0f \ud83c\udff3\ufe0f\u200d\ud83c\udf08",
            "Craig says: Don't talk to the feds"
        };

        return statuses[new Random().Next(statuses.Length)];
    }

    public string GetOperationStatus() => IsPaused() ? "Paused" : (IsSleeping() ? "Sleeping" : "Running");

    public Version GetVersion()
    {
        using var db = new BetaContext();
        return new Version(db.Configs.First(c => c.Name == "Version").Value);
    }

    public IEnumerable<ErrorServer> GetErrorServers()
    {
        using var db = new BetaContext();
        return db.ErrorServers;
    }

    public async Task CheckForUpdates()
    {
        await using var db = new BetaContext();
        var updates = new ConcurrentBag<Update>();

        foreach (var device in db.Devices.Where(d => d.Enabled))
        {
            try
            {
                var ups = await appleService.GetUpdate(device);
                foreach (var u in ups)
                {
                    if (appleService.ShouldPost(u, ref updates)) updates.Add(u);
                    else logger.LogInformation("No new update found for {updateName}", device.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                await DiscordService.PostError($"Error checking update for {device.FriendlyName}:\n{ex.Message}");
            }

            await Parallel.ForEachAsync(updates, async (update, _) =>
            {
                logger.LogInformation(
                    "Update for {DeviceFriendlyName} found. Version {UpdateVersionReadable} with build id {UpdateBuild}",
                    update.Device.FriendlyName, update.VersionReadable, update.Build);

                // save update to db
                appleService.SaveUpdate(update);

                // queue the update
                var postServers = db.Servers.Where(s => s.Category == update.Device.Category);
                foreach (var server in postServers)
                {
                    await PostUpdateNotification(server, update);
                }
            });
        }
    }

    public async Task PostUpdateNotification(Server server, Update update, bool skipExtras = false)
    {
        await using var db = new BetaContext();

        var postOld = bool.Parse(db.Configs.First(c => c.Name == "PostOld").Value);
        if (!postOld && update.ReleaseDate.DayOfYear != DateTime.Today.DayOfYear)
        {
            var error =
                $"{update.Device.FriendlyName} update {update.VersionReadable}-{update.Build} was released on {update.ReleaseDate.ToShortDateString()}. too old. not posting.";
            logger.LogInformation(error);
            await DiscordService.PostError(error);
        }

        var postedThreads = new List<string>();
        var postedForums = new List<string>();
        if (!skipExtras)
        {
            var threads = db.Threads.Where(t => t.Category == update.Device.Category && t.ServerId == server.ServerId);
            var forums = db.Forums.Where(f => f.Category == update.Device.Category && f.ServerId == server.ServerId);

            // post threads
            foreach (var thread in threads)
            {
                var post = await DiscordService.CreateThread(thread, update);
                postedThreads.Add($"https://discord.com/channels/{post.GuildId}/{post.Id}");
            }

            // post forums
            foreach (var forum in forums)
            {
                var post = await DiscordService.CreateForum(forum, update);
                postedForums.Add($"https://discord.com/channels/{post.GuildId}/{post.Id}");
            }
        }

        await DiscordService.PostUpdate(server, update, postedThreads, postedForums);
    }

    public string GetTier()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") switch
        {
            "Release" => "Prod",
            _ => "Dev"
        };
    }
}