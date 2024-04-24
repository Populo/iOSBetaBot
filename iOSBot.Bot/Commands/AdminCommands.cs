using System.Text;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using iOSBot.Service;
using NLog;

namespace iOSBot.Bot.Commands;

public static class AdminCommands
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static async void GetServers(SocketSlashCommand arg, DiscordRestClient bot)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
        }

        await arg.DeferAsync(ephemeral: true);
        var servers = await bot.GetGuildsAsync();
        StringBuilder response = new StringBuilder();
        response.AppendLine("Servers:");

        var i = 0;
        foreach (var s in servers)
        {
            response.AppendLine($"{i + 1}: {s.Name} (@{await s.GetOwnerAsync()})");
            ++i;
        }

        await arg.FollowupAsync(response.ToString());
    }

    

    public static async void FakeUpdate(SocketSlashCommand arg, IDiscordService discordService)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
        }

        await arg.DeferAsync();

        var category = (string)arg.Data.Options.First(o => o.Name == "category").Value;
        var fakeBuild = (string)arg.Data.Options.First(o => o.Name == "build").Value;
        var fakeVersion = (string)arg.Data.Options.First(o => o.Name == "version").Value;
        var fakeDocId = (string)arg.Data.Options.First(o => o.Name == "docid").Value;

        var db = new BetaContext();

        var device = db.Devices.FirstOrDefault(d => d.Category == category) ?? throw new Exception("Cannot find device");
        var servers = db.Servers.Where(s => s.Category == category);

        var fakeUpdate = new Service.Update()
        {
            Build = fakeBuild,
            Device = device,
            Version = fakeVersion,
            SizeBytes = 69420000000,
            ReleaseDate = DateTime.Today,
            ReleaseType = device.Type,
            VersionDocId = fakeDocId,
            Group = device.Category
        };

        foreach (var s in servers)
        {
            discordService.PostUpdate(fakeUpdate, s);
        }

        await arg.FollowupAsync("Posted update", ephemeral: true);
    }
    
    internal static async void YesErrors(SocketSlashCommand arg, DiscordRestClient? restClient)
    {
        if (!IsAllowed(arg.User.Id))
        { 
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
        }

        using var db = new BetaContext();

        var errorServer = db.ErrorServers.FirstOrDefault(s => s.ServerId == arg.GuildId!.Value && s.ChannelId == arg.ChannelId!.Value);
        if (null == errorServer)
        {
            db.ErrorServers.Add(new ErrorServer
            {
                ChannelId = arg.ChannelId!.Value,
                ServerId = arg.GuildId!.Value,
                Id = Guid.NewGuid()
            });

            await arg.RespondAsync("bot errors will now be posted here (if possible)", ephemeral: true);

            db.SaveChanges();
        }
        else
        {
            await arg.RespondAsync("Errors are already set to be posted here.", ephemeral:true);
        }
    }

    internal static async void NoErrors(SocketSlashCommand arg)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
        }

        using var db = new BetaContext();

        var errorServer = db.ErrorServers.FirstOrDefault(s => s.ServerId == arg.GuildId!.Value && s.ChannelId == arg.ChannelId!.Value);

        if (null == errorServer)
        {
            await arg.RespondAsync("bot errors were not set to go here", ephemeral: true);
        }
        else
        {
            db.ErrorServers.Remove(errorServer);

            await arg.RespondAsync("errors will not post here anymore", ephemeral: true);

            db.SaveChanges();
        }
    }

    internal static async void UpdateOptions(SocketSlashCommand arg, DiscordSocketClient bot)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
        }

        // watch + unwatch
        var commands = CommandInitializer.CommandBuilders.Where(c => c.Name.Contains("watch"));

        foreach (var c in commands)
        {
            c.Options.First(o => o.Name == "category").Choices = CommandInitializer.GetDeviceCategories();
        }

        var devices = CommandInitializer.CommandBuilders
            .First(c => c.Name == "watch").Options
            .First(c => c.Name == "category").Choices
            .Select(c => c.Name);


        Logger.Trace($"Devices to watch: {string.Join(" | ", devices)}");
        await arg.DeferAsync(ephemeral: true);
        
        CommandInitializer.UpdateCommands(bot);
        
        await arg.FollowupAsync("Reloaded commands.", ephemeral: true);
    }
    
    public static bool IsAllowed(ulong userId)
    {
        // only me
        return userId == 191051620430249984;
    }
}