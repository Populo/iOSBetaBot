using Discord;
using Discord.Rest;
using iOSBot.Service;

namespace iOSBot.Web.Service;

public interface IBotService
{
    DiscordServer GetServer(ulong id);
}

public class BotService : IBotService
{
      private DiscordRestClient _bot;

      public BotService()
      {
            string token;
            
#if DEBUG
            token = Environment.GetEnvironmentVariable("DevBetaBotToken");
#else
            token = Environment.GetEnvironmentVariable("BetaBotToken");
#endif

          _bot = new DiscordRestClient();
          _bot.LoginAsync(TokenType.Bot, token);
      }

      public DiscordServer GetServer(ulong id)
      {
          var server = _bot.GetGuildAsync(id).Result;
          return new DiscordServer
          {
              Id = id,
              Name = server.Name
          };
      }
}