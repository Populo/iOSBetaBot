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
        _bot = new DiscordRestClient();
        _bot.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BetaBotBotToken"));
    }


    public DiscordServer GetServerAndChannels(ulong serverId)
    {
        throw new NotImplementedException();
    }
}