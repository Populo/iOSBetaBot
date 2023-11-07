using Discord;
using Discord.Rest;

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
        throw new NotImplementedException();
    }
}