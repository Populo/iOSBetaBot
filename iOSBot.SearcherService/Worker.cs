using System.Collections.Concurrent;
using iOSBot.Data;
using iOSBot.Service;
using Update = iOSBot.Service.Update;

namespace iOSBot.SearcherService;

public class Worker : BackgroundService
{
    private readonly IAppleService _appleService;
    private readonly IBotService _botService;
    private readonly ILogger<Worker> _logger;
    private readonly IMqService _mqService;

    public Worker(ILogger<Worker> logger, IAppleService appleService, IBotService botService, IMqService mqService)
    {
        _logger = logger;
        _appleService = appleService;
        _botService = botService;
        _mqService = mqService;
    }

    private int RefreshTimer { get; set; } = 36000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            CheckForUpdates();
            await Task.Delay(RefreshTimer, stoppingToken);
        }
    }

    private async void CheckForUpdates()
    {
        var db = new BetaContext();
        RefreshTimer = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);

        if (_botService.IsSleeping()) return;

        // get updates
        var updates = new ConcurrentBag<Update>();
        var dbUpdates = db.Updates.ToList();

        await Parallel.ForEachAsync(db.Devices.Where(d => d.Enabled), async (device, _) =>
        {
            try
            {
                var ups = await _appleService.GetUpdate(device);
                foreach (var u in ups)
                {
                    /*
                     * cases:
                     * 1) new update
                     *      -> post as normal
                     * 2) nothing new
                     *      -> do nothing
                     * 3) re-release with same build
                     *      -> post
                     * 4) new release date but same hash
                     *      -> dont post
                     */

                    var post = false;
                    var existingDb = dbUpdates.Where(d => d.Category == u.Group && d.Build == u.Build)
                        .ToList();

                    // we've seen this build before
                    if (existingDb.Any())
                    {
                        // new hash of same update
                        if (!existingDb.Any(d => d.Hash == u.Hash))
                            // not already going to post this build found just now
                            if (!updates.Any(d => d.Group == u.Group && d.Build == u.Build))
                                post = true;
                    }
                    else
                    {
                        // not saved in db, but have we seen it yet this loop?
                        if (updates.Any(d => d.Group == u.Group && d.Build == u.Build))
                            // yes, save anyway even though we arent posting
                            _appleService.SaveUpdate(u);
                        else
                            // no, post it (and save later)
                            post = true;
                    }

                    if (post) updates.Add(u);
                    else _logger.LogInformation("No new update found for " + device.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                _mqService.QueueMessage("ERRORS", ex);
            }
        });

        // post updates
        foreach (var update in updates)
        {
            _logger.LogInformation(
                $"Update for {update.Device.FriendlyName} found. Version {update.VersionReadable} with build id {update.Build}");

            // dont post if older than 12 hours, still add to db tho
            var postOld = Convert.ToBoolean(Convert.ToInt16(db.Configs.First(c => c.Name == "PostOld").Value));

            // save update to db
            _appleService.SaveUpdate(update);

            if (!postOld && update.ReleaseDate.DayOfYear != DateTime.Today.DayOfYear)
            {
                var error = new
                {
                    Error =
                        $"{update.Device.FriendlyName} update {update.VersionReadable}-{update.Build} was released on {update.ReleaseDate.ToShortDateString()}. too old. not posting."
                };

                _logger.LogInformation(error.Error);
                _mqService.QueueMessage("ERRORS", error);
                continue;
            }

            _mqService.QueueMessage("UPDATE", update);
        }
    }
}