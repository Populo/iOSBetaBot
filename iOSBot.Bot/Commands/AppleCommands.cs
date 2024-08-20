using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using NLog;
using Thread = iOSBot.Data.Thread;

namespace iOSBot.Bot.Commands;

public class AppleCommands
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task YesWatch(SocketSlashCommand command, DiscordSocketClient client)
    {
        using var db = new BetaContext();

        await command.DeferAsync(ephemeral: true);

        var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.First().Value)
                     ?? throw new Exception("No device provided");

        var roleParam = command.Data.Options.FirstOrDefault(c => c.Name == "role");

        IRole? role = null;
        if (null != roleParam)
        {
            role = roleParam.Value as SocketRole;
        }

        var channel = await command.GetChannelAsync() as RestTextChannel
                      ?? throw new Exception("Could not get channel");
        var guild = client.GetGuild(channel.GuildId)
                    ?? throw new Exception("Could not get guild");

        if (db.Servers.Any(s => s.ChannelId == command.ChannelId
                                && s.ServerId == command.GuildId
                                && s.Category == device.Category))
        {
            await command.FollowupAsync($"You already receive {device.FriendlyName} updates in this channel.",
                ephemeral: true);
            return;
        }

        var server = new Server
        {
            ChannelId = channel.Id,
            ServerId = guild.Id,
            Id = Guid.NewGuid(),
            Category = device.Category,
            TagId = null == role ? "" : role.Id.ToString()
        };

        db.Servers.Add(server);

        db.SaveChanges();

        Logger.Info($"Signed up for {device.FriendlyName} updates in {guild.Name}:{channel.Name}");
        await command.FollowupAsync($"You will now receive {device.FriendlyName} updates in this channel.",
            ephemeral: true);
    }

    public static async Task NoWatch(SocketSlashCommand command, DiscordSocketClient client)
    {
        using var db = new BetaContext();
        await command.DeferAsync(ephemeral: true);

        var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.First().Value)
                     ?? throw new Exception("No device provided");
        var server = db.Servers.FirstOrDefault(s =>
            s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device.Category);

        var channel = await command.GetChannelAsync() as RestTextChannel
                      ?? throw new Exception("Could not get channel");
        var guild = client.GetGuild(channel.GuildId);

        if (null == server)
        {
            await command.FollowupAsync($"You were not receiving {device.FriendlyName} updates in this channel",
                ephemeral: true);
        }
        else
        {
            db.Servers.Remove(server);
            db.SaveChanges();

            Logger.Info(
                $"Removed notifications for {device.FriendlyName} updates in {guild.Name}:{channel.Name}");
            await command.FollowupAsync($"You will no longer receive {device.FriendlyName} updates in this channel",
                ephemeral: true);
        }
    }

    public static async Task YesThreads(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();
        var category = (string)arg.Data.Options.First().Value;
        var channel = await arg.GetChannelAsync() as RestTextChannel
                      ?? throw new Exception("Could not get channel");

        db.Threads.Add(new Thread()
        {
            Category = category,
            ChannelId = channel.Id,
            ServerId = channel.GuildId,
            id = Guid.NewGuid()
        });

        await db.SaveChangesAsync();

        await arg.FollowupAsync(text: "A release thread will be posted here.", ephemeral: true);
    }

    public static async Task NoThreads(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();
        var category = (string)arg.Data.Options.First().Value;

        var thread = db.Threads.FirstOrDefault(t => t.ChannelId == arg.ChannelId && t.Category == category);

        if (thread is null)
        {
            await arg.FollowupAsync(text: "Release threads were not set to be posted here", ephemeral: true);
            return;
        }

        db.Threads.Remove(thread);

        await db.SaveChangesAsync();

        await arg.FollowupAsync(text: "Release threads will no longer be posted here.", ephemeral: true);
    }

    public static async Task YesForum(SocketSlashCommand arg, DiscordSocketClient bot)
    {
        await arg.DeferAsync(ephemeral: true);

        var channel = arg.Data.Options.First(o => o.Name == "channel").Value;
        var category = arg.Data.Options.First(o => o.Name == "category").Value.ToString()
                       ?? throw new Exception("No category provided");

        if (null == channel || channel.GetType() != typeof(SocketForumChannel))
        {
            await arg.FollowupAsync("Please select a forum channel I can write to", ephemeral: true);
            return;
        }

        var forum = channel as SocketForumChannel ?? throw new Exception("Channel is not a forum channel");

        using var db = new BetaContext();
        var dbF = db.Forums.FirstOrDefault(f => f.Category == category && f.ChannelId == forum.Id);
        if (null != dbF)
        {
            await arg.FollowupAsync($"Forum posts will already happen for {category} in {forum.Name}", ephemeral: true);
            return;
        }

        db.Forums.Add(new Forum()
        {
            ChannelId = forum.Id,
            Category = category,
            ServerId = forum.Guild.Id,
            id = Guid.NewGuid()
        });

        await db.SaveChangesAsync();
        await arg.FollowupAsync("Forum posts will happen here", ephemeral: true);
    }

    public static async Task NoForum(SocketSlashCommand arg, DiscordSocketClient bot)
    {
        await arg.DeferAsync(ephemeral: true);

        var channel = arg.Data.Options.First(o => o.Name == "channel").Value;
        var category = arg.Data.Options.First(o => o.Name == "category").Value.ToString();

        if (null == channel || channel.GetType() != typeof(SocketForumChannel))
        {
            await arg.FollowupAsync("Please select a forum channel I can write to", ephemeral: true);
            return;
        }

        var forum = channel as SocketForumChannel ?? throw new Exception("Channel is not a forum channel");

        using var db = new BetaContext();
        var dbF = db.Forums.FirstOrDefault(f => f.Category == category && f.ChannelId == forum.Id);
        if (null == dbF)
        {
            await arg.FollowupAsync($"Forum posts are not set to happen for {category} in {forum.Name}",
                ephemeral: true);
            return;
        }

        db.Forums.Remove(dbF);
        await db.SaveChangesAsync();
        await arg.FollowupAsync($"Forum posts for {category} will no longer happen in {forum.Name}", ephemeral: true);
    }

    public static async Task DeviceInfo(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();
        var device = db.Devices.FirstOrDefault(d => d.Category == (string)arg.Data.Options.First().Value)
                     ?? throw new Exception("Cannot get device");
        var update = db.Updates
                         .Where(u => u.Category == device.Category)
                         .OrderByDescending(u => u.ReleaseDate)
                         .FirstOrDefault()
                     ?? throw new Exception("Cannot get latest update");

        var embed = new EmbedBuilder
        {
            Color = new Color(device.Color),
            Title = "Device Info",
            Description = $"{device.FriendlyName} feed"
        };
        embed.AddField(name: "Device", value: device.Name)
            .AddField(name: "Device Version", value: $"{device.Version} ({device.BuildId})")
            .AddField(name: "Newest Version", value: $"{update.Version} ({update.Build})");

        await arg.FollowupAsync(embed: embed.Build());
    }
}