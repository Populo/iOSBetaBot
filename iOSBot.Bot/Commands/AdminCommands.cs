﻿using System.Text;
using Discord.WebSocket;
using iOSBot.Bot.Helpers;
using iOSBot.Data;
using iOSBot.Service;
using Serilog;

namespace iOSBot.Bot.Commands;

public class AdminCommands
{
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

        await using var db = new BetaContext();

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

        await db.SaveChangesAsync();

        await arg.RespondAsync("bot errors will now be posted here (if possible)", ephemeral: true);
    }

    public static async Task NoErrors(SocketSlashCommand arg, DiscordSocketClient client)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        await using var db = new BetaContext();
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

            await db.SaveChangesAsync();
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
            Log.ForContext(typeof(AdminCommands)).Information("Updating commands");
            Log.ForContext(typeof(AdminCommands))
                .Information(string.Join(" | ", CommandObjects.CommandBuilders.Select(c => c.Name)));
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

    public static async Task FakeUpdate(SocketSlashCommand arg, ICraigService craig)
    {
        if (!IsAllowed(arg.User.Id))
        {
            await arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            return;
        }

        await arg.DeferAsync();

        var track = (string)arg.Data.Options.First(o => o.Name == "track").Value;
        var fakeBuild = (string)"42069f";
        var fakeVersion = (string)arg.Data.Options.First(o => o.Name == "version").Value;

        var db = new BetaContext();
        var internDb = new InternContext();

        var dbTrack = internDb.Tracks.First(t => t.Name == track);
        var servers = db.Servers.Where(s => s.Track == dbTrack.TrackId);

        var fakeUpdate = new MqUpdate()
        {
            Build = fakeBuild,
            Version = fakeVersion,
            ReleaseDate = DateTime.Today,
            Size = "69.42tb",
            TrackName = dbTrack.Name,
            ReleaseType = dbTrack.ReleaseType,
            TrackId = dbTrack.TrackId
        };

        foreach (var s in servers)
        {
            _ = craig.PostUpdateNotification(s, fakeUpdate);
        }

        await arg.FollowupAsync("Posted update", ephemeral: true);
    }

    public static bool IsAllowed(ulong userId)
    {
        // only me
        return userId == 191051620430249984;
    }
}