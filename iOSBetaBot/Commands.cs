using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using NLog;

namespace iOSBot.Bot
{
    public class Commands
    {
        public static Logger Logger = LogManager.GetCurrentClassLogger();

        public static void InitCommand(SocketSlashCommand command)
        {
            using var db = new BetaContext();

            command.DeferAsync(ephemeral: true);
            
            var category = Helpers.CategoryColors.FirstOrDefault(c => c.Category == (string)command.Data.Options.FirstOrDefault().Value);

            var server = db.Servers.FirstOrDefault(s => s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == category.Category);

            if (null == server)
            {
                server = new Server()
                {
                    ChannelId = command.ChannelId.Value,
                    ServerId = command.GuildId.Value,
                    Id = Guid.NewGuid(),
                    Category = category.Category
                };

                db.Servers.Add(server);

                db.SaveChanges();

                Logger.Info($"Signed up for {category.CategoryFriendly} updates in {command.Channel.Name}");
                command.FollowupAsync($"You will now receive {category.CategoryFriendly} updates in this channel.", ephemeral: true);
            }
            else
            {
                command.FollowupAsync($"You already receive {category.CategoryFriendly} updates in this channel.", ephemeral: true);
            }
        }

        internal static void RemoveCommand(SocketSlashCommand command)
        {
            using var db = new BetaContext();
            command.DeferAsync(ephemeral: true);

            var category = Helpers.CategoryColors.FirstOrDefault(c => c.Category == (string)command.Data.Options.FirstOrDefault().Value);
            var server = db.Servers.FirstOrDefault(s => s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == category.Category);

            if (null == server)
            {
                command.FollowupAsync($"You were not receiving {category.CategoryFriendly} updates in this channel", ephemeral: true);
            }
            else
            {
                db.Servers.Remove(server);
                db.SaveChanges();

                Logger.Info($"Removed notifications for {category.CategoryFriendly} updates in {command.Channel.Name}");
                command.FollowupAsync($"You will no longer receive {category.CategoryFriendly} updates in this channel", ephemeral: true);
            }
        }
    }
}
