using iOSBot.Console.Context;
using iOSBot.Data;
using Thread = iOSBot.Data.Thread;

namespace iOSBot.Console;

class Program
{
    static void Main(string[] args) => new Program().Run();

    public void Run()
    {
        System.Console.WriteLine("Initializing...");
        using var newDb = new BetaContext();
        using var oldDb = new MyDbContext();
        using var internDb = new InternContext();

        Dictionary<string, Guid> categoriesToTrack = new()
        {
            { "iOSDev", Guid.Parse("1079e46f-a4e8-44cf-9988-c84e04236feb") },
            { "audioOSDev", Guid.Parse("148ad01c-3c35-4496-a6dc-6cb7e45c24bc") },
            { "iOSPublic", Guid.Parse("8c37947e-ee5a-4382-ae3d-a0ef818ea6e2") },
            { "iOSRetail", Guid.Parse("6a89f6c2-7b1a-4dd2-a92f-ac477dc87ebf") },
            { "tvOSDev", Guid.Parse("f5669127-3c3a-433f-93e9-84c5f5384b04") },
            { "VisionOSDevBeta", Guid.Parse("65b441a6-ff90-47ae-9522-06fff03dbbfe") },
            { "watchOSDev", Guid.Parse("16d70b3b-d29c-44b6-bdb0-12d7bcf4851e") },
            { "macOSDev", Guid.Parse("aaffb56b-2b2e-4adf-a2f7-e2b138c9ffc7") },
            { "xrOSStable", Guid.Parse("3d336c4e-a3ae-48bf-baf1-5b581558db9e") }
        };

        System.Console.WriteLine("Initialized");
        System.Console.WriteLine("migrating servers...");
        foreach (var server in oldDb.Servers)
        {
            newDb.Servers.Add(new Server()
            {
                Track = categoriesToTrack[server.Category],
                ServerId = server.ServerId,
                ChannelId = server.ChannelId,
                Id = server.Id,
                TagId = server.TagId,
            });
        }

        System.Console.WriteLine("servers migrated.");
        System.Console.WriteLine("migrating threads...");

        foreach (var thread in oldDb.Threads)
        {
            newDb.Threads.Add(new Thread()
            {
                Track = categoriesToTrack[thread.Category],
                ServerId = thread.ServerId,
                ChannelId = thread.ChannelId,
                id = thread.Id
            });
        }

        System.Console.WriteLine("threads migrated.");
        System.Console.WriteLine("migrating forums...");

        foreach (var forum in oldDb.Forums)
        {
            newDb.Forums.Add(new Forum()
            {
                Track = categoriesToTrack[forum.Category],
                ServerId = forum.ServerId,
                ChannelId = forum.ChannelId,
                id = forum.Id
            });
        }

        System.Console.WriteLine("forums migrated.");

        // System.Console.WriteLine("migrating updates...");
        // foreach (var update in oldDb.Updates)
        // {
        //     newDb.Add(new iOSBot.Data.Update()
        //     {
        //         TrackId = categoriesToTrack[update.Category],
        //         Version = update.Version,
        //         Build = update.Build,
        //         ReleaseDate = update.ReleaseDate,
        //         Hash = update.Hash,
        //         SUDocId = "nah",
        //         UpdateId = update.Guid
        //     });
        // }
        // System.Console.WriteLine("updates migrated.");

        System.Console.WriteLine("migrating configs...");
        foreach (var c in oldDb.Configs)
        {
            newDb.Configs.Add(new Config()
            {
                Name = c.Name,
                Value = c.Value
            });
        }

        System.Console.WriteLine("configs migrated.");

        newDb.SaveChanges();
    }
}