using System.Text;
using Discord;
using Discord.WebSocket;
using iOSBot.Bot.Helpers;
using iOSBot.Data;
using Serilog;
using Thread = iOSBot.Data.Thread;

namespace iOSBot.Bot.Commands;

public class AppleCommands
{
    public static async Task YesWatch(SocketSlashCommand command, DiscordSocketClient client)
    {
        await using var db = new BetaContext();
        await using var internDb = new InternContext();

        await command.DeferAsync(ephemeral: true);
        var trackId = (string)command.Data.Options.First().Value;

        var track = internDb.Tracks.FirstOrDefault(d => d.TrackId.ToString() == trackId)
                    ?? throw new Exception("Not a valid track");

        var roleParam = command.Data.Options.FirstOrDefault(c => c.Name == "role");

        IRole? role = null;
        if (null != roleParam)
        {
            role = roleParam.Value as SocketRole;
        }

        SocketTextChannel channel;
        SocketGuild guild;
        CommandObjects.GetChannelAndGuild(command, client, out guild, out channel);

        if (db.Servers.Any(s => s.ChannelId == channel.Id
                                && s.ServerId == guild.Id
                                && s.Track == track.TrackId))
        {
            await command.FollowupAsync($"You already receive {track.Name} updates in this channel.",
                ephemeral: true);
            return;
        }

        var server = new Server
        {
            ChannelId = channel.Id,
            ServerId = guild.Id,
            Id = Guid.NewGuid(),
            Track = track.TrackId,
            TagId = null == role ? "" : role.Id.ToString()
        };

        db.Servers.Add(server);

        await db.SaveChangesAsync();

        Log.ForContext<AppleCommands>()
            .Information(
                "Signed up for {TrackName} updates in {GuildName}:{ChannelName}", track.Name, guild.Name, channel.Name);
        await command.FollowupAsync($"You will now receive {track.Name} updates in this channel.",
            ephemeral: true);
    }

    public static async Task NoWatch(SocketSlashCommand command, DiscordSocketClient client)
    {
        await using var db = new BetaContext();
        await using var internDb = new InternContext();
        await command.DeferAsync(ephemeral: true);
        var trackId = (string)command.Data.Options.First().Value;

        var track = internDb.Tracks.FirstOrDefault(d => d.TrackId.ToString() == trackId)
                    ?? throw new Exception("Not a valid track");
        var server = db.Servers.FirstOrDefault(s =>
            s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Track.ToString() == trackId);

        SocketTextChannel channel;
        SocketGuild guild;
        CommandObjects.GetChannelAndGuild(command, client, out guild, out channel);

        if (null == server)
        {
            await command.FollowupAsync($"You were not receiving {track.Name} updates in this channel",
                ephemeral: true);
        }
        else
        {
            db.Servers.Remove(server);
            await db.SaveChangesAsync();

            Log.ForContext<AppleCommands>().Information(
                "Removed notifications for {TrackName} updates in {GuildName}:{ChannelName}", track.Name, guild.Name,
                channel.Name);
            await command.FollowupAsync($"You will no longer receive {track.Name} updates in this channel",
                ephemeral: true);
        }
    }

    public static async Task YesThreads(SocketSlashCommand command, DiscordSocketClient client)
    {
        await command.DeferAsync(ephemeral: true);

        await using var db = new BetaContext();
        await using var internDb = new InternContext();
        var category = (string)command.Data.Options.First().Value;

        SocketTextChannel channel;
        SocketGuild guild;
        CommandObjects.GetChannelAndGuild(command, client, out guild, out channel);
        var dbTrack = internDb.Tracks.First(t => t.TrackId.ToString() == category);

        db.Threads.Add(new Thread()
        {
            Track = dbTrack.TrackId,
            ChannelId = channel.Id,
            ServerId = guild.Id,
            id = Guid.NewGuid()
        });

        await db.SaveChangesAsync();

        await command.FollowupAsync(text: "A release thread will be posted here.", ephemeral: true);
    }

    public static async Task NoThreads(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);

        await using var db = new BetaContext();
        var track = (string)arg.Data.Options.First().Value;

        var thread = db.Threads.FirstOrDefault(t => t.ChannelId == arg.ChannelId && t.Track.ToString() == track);

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
        var trackId = arg.Data.Options.First(o => o.Name == "category").Value.ToString()
                      ?? throw new Exception("No category provided");

        if (null == channel || channel.GetType() != typeof(SocketForumChannel))
        {
            await arg.FollowupAsync("Please select a forum channel I can write to", ephemeral: true);
            return;
        }

        var forum = channel as SocketForumChannel ?? throw new Exception("Channel is not a forum channel");

        await using var db = new BetaContext();
        await using var internDb = new InternContext();

        var track = internDb.Tracks.First(t => t.TrackId.ToString() == trackId);
        var dbF = db.Forums.FirstOrDefault(f => f.Track == track.TrackId && f.ChannelId == forum.Id);
        if (null != dbF)
        {
            await arg.FollowupAsync($"Forum posts will already happen for {track.Name} in {forum.Name}",
                ephemeral: true);
            return;
        }

        db.Forums.Add(new Forum()
        {
            ChannelId = forum.Id,
            Track = track.TrackId,
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
        var trackId = arg.Data.Options.First(o => o.Name == "category").Value.ToString();

        if (null == channel || channel.GetType() != typeof(SocketForumChannel))
        {
            await arg.FollowupAsync("Please select a forum channel I can write to", ephemeral: true);
            return;
        }

        var forum = channel as SocketForumChannel ?? throw new Exception("Channel is not a forum channel");

        await using var db = new BetaContext();
        await using var internDb = new InternContext();

        var track = internDb.Tracks.First(t => t.TrackId.ToString() == trackId);
        var dbF = db.Forums.FirstOrDefault(f => f.Track == track.TrackId && f.ChannelId == forum.Id);
        if (null == dbF)
        {
            await arg.FollowupAsync($"Forum posts are not set to happen for {track.Name} in {forum.Name}",
                ephemeral: true);
            return;
        }

        db.Forums.Remove(dbF);
        await db.SaveChangesAsync();
        await arg.FollowupAsync($"Forum posts for {track.Name} will no longer happen in {forum.Name}", ephemeral: true);
    }

    public static async Task ArchiveCommand(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);

        var trackId = arg.Data.Options.First(o => o.Name == "category").Value.ToString();
        var version = arg.Data.Options.First(o => o.Name == "version").Value.ToString();

        using var db = new InternContext();

        var track = db.Tracks.FirstOrDefault(t => t.TrackId == Guid.Parse(trackId))
                    ?? throw new Exception($"Could not get track with id {trackId}");
        // trailing space to get only this version
        // 8.4 -> returns 8.4 beta 1, 2, 3 etc but not 8.4.1 or 8.4.2
        var updates = db.Updates.Where(u => u.TrackId == track.TrackId &&
                                            u.Version.Contains($"{version} "));

        if (!updates.Any())
        {
            await arg.FollowupAsync("Could not find any updates.");
            return;
        }

        int longestVersion = updates
            .OrderByDescending(u => u.Version.Length)
            .First()
            .Version
            .Length;
        int longestBuild = updates
            .OrderByDescending(u => u.Build.Length)
            .First()
            .Build
            .Length;
        var dates = updates.Select(u => u.ReleaseDate.ToString("dddd MMMM dd, yyyy"));
        int longestDate = dates
            .OrderByDescending(u => u.Length)
            .First()
            .Length;

        string titleVersion = "Version",
            titleBuild = "Build",
            titleDate = "Release Date";

        longestVersion = longestVersion < titleVersion.Length ? titleVersion.Length : longestVersion;
        longestBuild = longestBuild < titleBuild.Length ? titleBuild.Length : longestBuild;
        longestDate = longestDate < titleDate.Length ? titleDate.Length : longestDate;

        var versionHeader = titleVersion.CenterString(longestVersion);
        var buildHeader = titleBuild.CenterString(longestBuild);
        var dateHeader = titleDate.CenterString(longestDate);

        var header = $"| {versionHeader} | {buildHeader} | {dateHeader} |";
        var builder = new StringBuilder();

        builder.AppendLine("```");
        builder.AppendLine(
            $"\u256d{string.Concat(Enumerable.Repeat('\u2015', longestVersion + 2))}\u252c{string.Concat(Enumerable.Repeat('\u2015', longestBuild + 2))}\u252c{string.Concat(Enumerable.Repeat('\u2015', longestDate + 2))}\u256e");
        builder.AppendLine(header);
        builder.AppendLine(
            $"\u251c{string.Concat(Enumerable.Repeat('\u2015', longestVersion + 2))}\u007c{string.Concat(Enumerable.Repeat('\u2015', longestBuild + 2))}\u007c{string.Concat(Enumerable.Repeat('\u2015', longestDate + 2))}\u2524");
        var sorted = updates.OrderByDescending(u => u.ReleaseDate);

        foreach (var up in sorted)
        {
            builder.AppendLine(
                $"| {up.Version.CenterString(longestVersion)} | {up.Build.CenterString(longestBuild)} | {up.ReleaseDate.ToString("dddd MMMM dd, yyyy").CenterString(longestDate)} |");
        }

        builder.AppendLine(
            $"\u2570{string.Concat(Enumerable.Repeat('\u2015', longestVersion + 2))}\u2534{string.Concat(Enumerable.Repeat('\u2015', longestBuild + 2))}\u2534{string.Concat(Enumerable.Repeat('\u2015', longestDate + 2))}\u256f");
        builder.Append("```");

        await arg.FollowupAsync(builder.ToString(), ephemeral: true);
    }
}