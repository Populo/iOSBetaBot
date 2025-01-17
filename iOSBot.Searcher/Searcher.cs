using System.Collections.Concurrent;
using System.Timers;
using iOSBot.Data;
using iOSBot.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using Timer = System.Timers.Timer;
using Update = iOSBot.Service.Update;

namespace iOSBot.Searcher;

public class Searcher
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddTransient<IBotService, BotService>();
        builder.Services.AddTransient<IAppleService, AppleService>();
        var host = builder.Build();
        
        // i dont think this is right but it works
        new Searcher().Run(host.Services);
    }
    
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    
    IBotService _botService;
    IAppleService _appleService;
    
    private Timer searchTimer { get; set; }

    
    private void Run(IServiceProvider serviceProvider)
    {
        _botService = serviceProvider.GetRequiredService<IBotService>();
        _appleService = serviceProvider.GetRequiredService<IAppleService>();
        
        switch (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
        {
            case "Release":
                _logger.Info("Environment: Prod");
                break;
            case "Develop":
                _logger.Info("Environment: Dev");
                break;
        }
        
        AppDomain.CurrentDomain.UnhandledException += (_, e) => { _logger.Error($"{e}\n{e.ExceptionObject}"); };

        searchTimer = new Timer()
        {
            AutoReset = true,
            Enabled = false,
            Interval = 1000 * 60 * 2 // 2 minutes
        };;
        searchTimer.Elapsed += SearchTimerOnElapsed;
        
        using (var db = new BetaContext())
        {
            searchTimer.Interval = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);
        }
    }

    private async Task SearchTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_botService.IsSleeping()) return;
        using var db = new BetaContext();
        
        // get updates
        var updates = new ConcurrentBag<Update>();
        var dbUpdates = db.Updates.ToList();
        
        await Parallel.ForEachAsync(db.Devices.Where(d => d.Enabled), async (device, _) =>
        {
            try
            {
               var ups = await AppleService.GetUpdate(device);
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
                        {
                            // not already going to post this build found just now
                            if (!updates.Any(d => d.Group == u.Group && d.Build == u.Build))
                            {
                                post = true;
                            }
                        }
                    }
                    else
                    {
                        // not saved in db, but have we seen it yet this loop?
                        if (updates.Any(d => d.Group == u.Group && d.Build == u.Build))
                        {
                            // yes, save anyway even though we arent posting
                            AppleService.SaveUpdate(u);
                        }
                        else
                        {
                            // no, post it (and save later)
                            post = true;
                        }
                    }

                    if (post) updates.Add(u);
                    else _logger.Info("No new update found for " + device.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                Poster.StaticError(UpdatePoster,
                    $"Error checking update for {device.FriendlyName}:\n{ex.Message}");
            }
        });
    }
}