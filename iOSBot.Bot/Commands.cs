using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using NLog;

namespace iOSBot.Bot
{
    public class Commands
    {
        public static Logger Logger = LogManager.GetCurrentClassLogger();

        public static void InitCommand(SocketSlashCommand command, DiscordRestClient client)
        {
            using var db = new BetaContext();

            command.DeferAsync(ephemeral: true);

            var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.FirstOrDefault().Value);

            var roleParam = command.Data.Options.FirstOrDefault(c => c.Name == "role");
            IRole role = null;
            if (null != roleParam)
            {
                role = roleParam.Value as SocketRole;
            }

            var channel = client.GetChannelAsync(command.ChannelId.Value).Result as RestTextChannel;
            var guild = client.GetGuildAsync(command.GuildId.Value).Result;

            var server = db.Servers.FirstOrDefault(s => s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device.Category);

            if (null == server)
            {
                server = new Server()
                {
                    ChannelId = command.ChannelId.Value,
                    ServerId = command.GuildId.Value,
                    Id = Guid.NewGuid(),
                    Category = device.Category,
                };

                server.TagId = null == role ? "" : role.Id.ToString();

                db.Servers.Add(server);

                db.SaveChanges();

                Logger.Info($"Signed up for {device.FriendlyName} updates in {guild.Name}:{channel.Name}");
                command.FollowupAsync($"You will now receive {device.FriendlyName} updates in this channel.", ephemeral: true);
            }
            else
            {
                command.FollowupAsync($"You already receive {device.FriendlyName} updates in this channel.", ephemeral: true);
            }
        }

        internal static void RemoveCommand(SocketSlashCommand command, DiscordRestClient client)
        {
            using var db = new BetaContext();
            command.DeferAsync(ephemeral: true);

            var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.FirstOrDefault().Value);
            var server = db.Servers.FirstOrDefault(s => s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device.Category);
            var channel = client.GetChannelAsync(command.ChannelId.Value).Result as RestTextChannel;
            var guild = client.GetGuildAsync(command.GuildId.Value).Result;

            if (null == server)
            {
                command.FollowupAsync($"You were not receiving {device.FriendlyName} updates in this channel", ephemeral: true);
            }
            else
            {
                db.Servers.Remove(server);
                db.SaveChanges();

                Logger.Info($"Removed notifications for {device.FriendlyName} updates in {guild.Name}:{channel.Name}");
                command.FollowupAsync($"You will no longer receive {device.FriendlyName} updates in this channel", ephemeral: true);
            }
        }

        internal static void ForceCommand(SocketSlashCommand command, DiscordRestClient client)
        {
            using var db = new BetaContext();
            command.DeferAsync(ephemeral: true);

            ApiSingleton.Instance.Timer_Elapsed(null, null);

            Logger.Info($"Update forced by {command.User.GlobalName}");
            command.FollowupAsync("Updates checked.");
        }

        internal static void ErrorCommand(SocketSlashCommand arg, DiscordRestClient restClient)
        {
            if (!IsAllowed(arg.User.Id))
            { 
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            }

            using var db = new BetaContext();

            var ErrorServer = db.ErrorServers.FirstOrDefault(s => s.ServerId == arg.GuildId.Value && s.ChannelId == arg.ChannelId.Value);
            if (null == ErrorServer)
            {
                db.ErrorServers.Add(new ErrorServer
                {
                    ChannelId = arg.ChannelId.Value,
                    ServerId = arg.GuildId.Value,
                    Id = Guid.NewGuid()
                });

                arg.RespondAsync("bot errors will now be posted here (if possible)", ephemeral: true);

                db.SaveChanges();
            }
            else
            {
                arg.RespondAsync("Errors are already set to be posted here.", ephemeral:true);
            }
        }

        internal static void RemoveErrorCommand(SocketSlashCommand arg, DiscordRestClient restClient)
        {
            if (!IsAllowed(arg.User.Id))
            {
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            }

            using var db = new BetaContext();

            var ErrorServer = db.ErrorServers.FirstOrDefault(s => s.ServerId == arg.GuildId.Value && s.ChannelId == arg.ChannelId.Value);

            if (null == ErrorServer)
            {
                arg.RespondAsync("bot errors were not set to go here", ephemeral: true);
            }
            else
            {
                db.ErrorServers.Remove(ErrorServer);

                arg.RespondAsync("errors will not post here anymore", ephemeral: true);

                db.SaveChanges();
            }
        }

        private static bool IsAllowed(ulong userId)
        {
            // only me
            return userId == 191051620430249984;
        }
    }
}
