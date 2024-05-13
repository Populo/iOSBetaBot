using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using iOSBot.Service.Models;
using Thread = iOSBot.Data.Thread;

namespace iOSBot.Service;

public interface IDiscordService
{
    public DiscordServer GetServerAndChannels(ulong serverId);
    public void PostToServers(DiscordSocketClient client, IEnumerable<ulong> serverIds, string message);
    public void PostUpdate(Update update, Server server);
    public void PostThread(Update update, Thread thread);
}

public class DiscordService : IDiscordService
{
    private DiscordRestClient _bot { get; set; }
    
    private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public DiscordService()
    {
        string token;
#if DEBUG
        token = Environment.GetEnvironmentVariable("BetaBotBotDevToken");
#else
        token = Environment.GetEnvironmentVariable("BetaBotBotToken");
#endif
        
        _bot = new DiscordRestClient();
        _bot.LoginAsync(TokenType.Bot, token);
    }


    public DiscordServer GetServerAndChannels(ulong serverId)
    {
        using var db = new BetaContext();
        var server = _bot.GetGuildAsync(serverId).Result;

        return new DiscordServer()
        {
            Name = server.Name,
            Id = server.Id,
            Channels = db.Servers.Where(s => s.ServerId == serverId)
                .AsEnumerable()
                .Select(s => new DiscordChannel()
                {
                    Id = s.ChannelId,
                    Name = server.GetChannelAsync(s.ChannelId).Result.Name,
                    Category = s.Category
                }).ToList()

        };
    }

    public async void PostToServers(DiscordSocketClient client, IEnumerable<ulong> serverIds, string message) =>
        PostToServers(client.Rest, serverIds, message);

    public async void PostToServers(DiscordRestClient client, IEnumerable<ulong> serverIds, string message)
    {
        foreach (var id in serverIds)
        {
            var channel = await client.GetChannelAsync(id) as ITextChannel;
            
            if (null != channel) await channel.SendMessageAsync(message);
        }
    }

    public async void PostUpdate(Update update, Server server)
    {
        using var db = new BetaContext();
        var channel = await _bot.GetChannelAsync(server.ChannelId) as ITextChannel;

        if (null == channel) return;

        var mention = server.TagId != "" ? $"<@&{server.TagId}>" : "";

        var embed = new EmbedBuilder
        {
            Color = new Color(update.Device.Color),
            Title = $"New {update.Device.FriendlyName} Release!",
            Timestamp = DateTime.Now,
        };
        embed.AddField(name: "Version", value: update.VersionReadable)
            .AddField(name: "Build", value: update.Build)
            .AddField(name: "Size", value: update.Size);

        if (!string.IsNullOrEmpty(update.Device.Changelog))
        {
            embed.Url = update.Device.Changelog;
        }

        Logger.Info($"Posting {update.VersionReadable} to {channel.Name}");
        try
        {
            await channel.SendMessageAsync(text: mention, embed: embed.Build());
        }
        catch (Exception e)
        {
            Logger.Error(e);
            PostToServers(_bot, db.ErrorServers.Select(s => s.ChannelId), $"Error posting to {channel.Name}. {e.Message}");
        }
    }

    public async void PostThread(Update update, Thread thread)
    {
        var channel = await _bot.GetChannelAsync(thread.ChannelId) as ITextChannel;
        Logger.Info($"Creating thread in {channel} for {update.VersionReadable}");

        await channel.CreateThreadAsync($"{update.VersionReadable} Release Thread");
        Logger.Info("Thread Created.");
    }
}