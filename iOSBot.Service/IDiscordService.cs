using Discord;
using Discord.Rest;
using iOSBot.Data;

namespace iOSBot.Service;

public interface IDiscordService
{
    public DiscordServer GetServerAndChannels(ulong serverId);
}

public class DiscordService : IDiscordService
{
    private DiscordRestClient _bot { get; set; }

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
}