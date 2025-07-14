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
    string GetVersion();
    Task PostUpdateNotification(Server server, MqUpdate update, bool skipExtras = false);
    string GetTier();
}

public class CraigService(
    ILogger<CraigService> logger,
    IDiscordService discordService)
    : ICraigService
{
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

    public string GetVersion()
    {
        using var db = new BetaContext();
        return db.Configs.First(c => c.Name == "Version").Value;
    }


    public async Task PostUpdateNotification(Server server, MqUpdate update, bool skipExtras = false)
    {
        await using var craigDb = new BetaContext();
        var postedThreads = new List<string>();
        var postedForums = new List<string>();

        try
        {
            if (!skipExtras)
            {
                var threads = craigDb.Threads.Where(t => t.Track == update.TrackId && t.ServerId == server.ServerId);
                var forums = craigDb.Forums.Where(f => f.Track == update.TrackId && f.ServerId == server.ServerId);

                // post threads
                foreach (var thread in threads)
                {
                    var post = await discordService.CreateThread(thread, update);
                    postedThreads.Add($"https://discord.com/channels/{post.GuildId}/{post.Id}");
                }

                // post forums
                foreach (var forum in forums)
                {
                    var post = await discordService.CreateForum(forum, update);
                    postedForums.Add($"https://discord.com/channels/{post.GuildId}/{post.Id}");
                }
            }

            await discordService.PostUpdate(server, update, postedThreads, postedForums);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error posting update");
            await discordService.PostError($"Error posting update:\n{e}");
        }
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