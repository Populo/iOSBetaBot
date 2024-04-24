using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using NLog;

namespace iOSBot.Bot.Commands;

public static class AppleCommands {
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static async void YesWatch(SocketSlashCommand command, DiscordRestClient client)
    {
        using var db = new BetaContext();

        await command.DeferAsync(ephemeral: true);

        var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.First().Value);

        var roleParam = command.Data.Options.FirstOrDefault(c => c.Name == "role");
        IRole? role = null;
        if (null != roleParam)
        {
            role = roleParam.Value as SocketRole;
        }

        var channel = client.GetChannelAsync(command.ChannelId!.Value).Result as RestTextChannel;
        var guild = client.GetGuildAsync(command.GuildId!.Value).Result;

        var server = db.Servers.FirstOrDefault(s => s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device!.Category);

        if (null == server)
        {
            server = new Server
            {
                ChannelId = command.ChannelId.Value,
                ServerId = command.GuildId.Value,
                Id = Guid.NewGuid(),
                Category = device!.Category,
                TagId = null == role ? "" : role.Id.ToString()
            };

            db.Servers.Add(server);

            db.SaveChanges();

            Logger.Info($"Signed up for {device.FriendlyName} updates in {guild.Name}:{channel!.Name}");
            await command.FollowupAsync($"You will now receive {device.FriendlyName} updates in this channel.", ephemeral: true);
        }
        else
        {
            await command.FollowupAsync($"You already receive {device!.FriendlyName} updates in this channel.", ephemeral: true);
        }
    }

    internal static async void NoWatch(SocketSlashCommand command, DiscordRestClient client)
    {
        using var db = new BetaContext();
        await command.DeferAsync(ephemeral: true);

        var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.First().Value);
        var server = db.Servers.FirstOrDefault(s => s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device!.Category);
        var channel = client.GetChannelAsync(command.ChannelId!.Value).Result as RestTextChannel;
        var guild = client.GetGuildAsync(command.GuildId!.Value).Result;

        if (null == server)
        {
            await command.FollowupAsync($"You were not receiving {device!.FriendlyName} updates in this channel", ephemeral: true);
        }
        else
        {
            db.Servers.Remove(server);
            db.SaveChanges();

            Logger.Info($"Removed notifications for {device!.FriendlyName} updates in {guild.Name}:{channel!.Name}");
            await command.FollowupAsync($"You will no longer receive {device.FriendlyName} updates in this channel", ephemeral: true);
        }
    }
    
    public static async void NewThreadChannel(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();
        var category = (string)arg.Data.Options.First().Value;

        if (null != arg.ChannelId && null != arg.GuildId)
        {
            db.Threads.Add(new Data.Thread()
            {
                Category = category,
                ChannelId = arg.ChannelId.Value,
                ServerId = arg.GuildId.Value,
                id = Guid.NewGuid()
            });
        }
        else
        {
            throw new Exception("cannot create thread");
        }

        db.SaveChanges();

        await arg.FollowupAsync(text: "A release thread will be posted here.", ephemeral: true);
    }

    public static async void DeleteThreadChannel(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);
        var category = (string)arg.Data.Options.First().Value;

        using var db = new BetaContext();

        var thread = db.Threads.FirstOrDefault(t => t.ChannelId == arg.ChannelId && t.Category == category);

        if (thread is null)
        {
            await arg.FollowupAsync(text: "Release threads were not set to be posted here", ephemeral: true);
            return;
        }

        db.Threads.Remove(thread);

        db.SaveChanges();

        await arg.FollowupAsync(text: "Release threads will no longer be posted here.", ephemeral: true);
    }
    
    public static async void DeviceInfo(SocketSlashCommand arg)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();
        var device = db.Devices.FirstOrDefault(d => d.Category == (string)arg.Data.Options.First().Value) ?? throw new Exception("Cannot find device");
        var update = db.Updates
            .Where(u => u.Category == device.Category)
            .OrderByDescending(u => u.ReleaseDate)
            .FirstOrDefault() ??
                     throw new Exception("Cannot find most recent update");
        
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