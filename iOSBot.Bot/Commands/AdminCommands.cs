using System.Text;
using Discord.WebSocket;
using iOSBot.Bot.Helpers;
using iOSBot.Data;
using NLog;
using Update = iOSBot.Service.Update;

namespace iOSBot.Bot.Commands;

public class AdminCommands
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task YesErrors(SocketSlashCommand arg, DiscordSocketClient client)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        SocketTextChannel channel;
        SocketGuild guild;
        CommandObjects.GetChannelAndGuild(arg, client, out guild, out channel);

        using var db = new BetaContext();

        if (db.ErrorServers.Any(s =>
                s.ServerId == guild.Id && s.ChannelId == channel.Id))
        {
            await arg.RespondAsync("Errors are already set to be posted here.", ephemeral: true);
            return;
        }

        db.ErrorServers.Add(new ErrorServer
        {
            ChannelId = channel.Id,
            ServerId = guild.Id,
            Id = Guid.NewGuid()
        });

        db.SaveChanges();

        await arg.RespondAsync("bot errors will now be posted here (if possible)", ephemeral: true);
    }

    public static async Task NoErrors(SocketSlashCommand arg, DiscordSocketClient client)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        using var db = new BetaContext();
        SocketTextChannel channel;
        SocketGuild guild;
        CommandObjects.GetChannelAndGuild(arg, client, out guild, out channel);

        var errorServer = db.ErrorServers.FirstOrDefault(s =>
            s.ServerId == guild.Id && s.ChannelId == channel.Id);

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

    public static async Task UpdateCommands(DiscordSocketClient client, SocketSlashCommand? arg, bool boot = false)
    {
        if (!boot && !IsAllowed(arg!.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
        }
        else
        {
            Logger.Info($"Updating commands");
            Logger.Trace(string.Join(" | ", CommandObjects.CommandBuilders.Select(c => c.Name)));
            await client.BulkOverwriteGlobalApplicationCommandsAsync(
                // ReSharper disable once CoVariantArrayConversion
                CommandObjects.CommandBuilders.Select(b => b.Build()).ToArray());

            if (!boot) await arg!.RespondAsync("Commands updated.", ephemeral: true);
        }
    }

    public static async Task GetServers(SocketSlashCommand arg, DiscordSocketClient bot)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        await arg.DeferAsync(ephemeral: true);
        var servers = bot.Guilds.OrderBy(g => g.Name).ToArray();
        var totalMembers = 0;
        var response = new StringBuilder();
        response.AppendLine("Servers:");

        for (int i = 0; i < servers.Length; ++i)
        {
            var members = servers[i].MemberCount;
            totalMembers += members;
            response.AppendLine(
                $"{i + 1}: {servers[i].Name} (@{await bot.GetUserAsync(servers[i].OwnerId)}) | {members} Members");
        }

        response.AppendLine($"\nServicing {servers.Length} servers with a total of {totalMembers} members.");

        await arg.FollowupAsync(response.ToString());
    }

    public static async Task FakeUpdate(SocketSlashCommand arg, Poster poster)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        await arg.DeferAsync();

        var category = (string)arg.Data.Options.First(o => o.Name == "category").Value;
        var fakeBuild = (string)arg.Data.Options.First(o => o.Name == "build").Value;
        var fakeVersion = (string)arg.Data.Options.First(o => o.Name == "version").Value;
        var fakeDocId = (string)arg.Data.Options.First(o => o.Name == "docid").Value;

        var db = new BetaContext();

        var device = db.Devices.FirstOrDefault(d => d.Category == category)
                     ?? throw new Exception("cant get device");
        var servers = db.Servers.Where(s => s.Category == category);

        var fakeUpdate = new Update()
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
            _ = poster.PostUpdateAsync(s, fakeUpdate);
        }

        await arg.FollowupAsync("Posted update", ephemeral: true);
    }

    public static async Task ToggleDevice(SocketSlashCommand arg)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        await arg.DeferAsync();

        var category = (string)arg.Data.Options.First(o => o.Name == "category").Value;
        var enable = (bool)arg.Data.Options.First(o => o.Name == "enable").Value;

        using var db = new BetaContext();

        var device = db.Devices.Where(d => d.Category == category);
        foreach (var d in device)
        {
            d.Enabled = enable;
        }

        await db.SaveChangesAsync();

        var enabled = enable ? "enabled" : "disabled";
        await arg.RespondAsync($"Category {category} is now {enabled}");
    }

    public static bool IsAllowed(ulong userId)
    {
        // only me
        return userId == 191051620430249984;
    }
}