using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using iOSBot.Service;
using Newtonsoft.Json;
using NLog;

namespace iOSBot.Bot
{
    public class Commands
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        #region command objects

        private static SlashCommandBuilder watchBuilder = new()
        {
            Name = "watch",
            Description = "Begin posting OS updates to this channel",
            DefaultMemberPermissions = GuildPermission.ManageGuild,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new ()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                },
                new ()
                {
                    Name = "role",
                    Description = "Role to ping",
                    IsRequired = false,
                    Type = ApplicationCommandOptionType.Role
                }
            }
        };

        private static SlashCommandBuilder unwatchBuilder = new()
        {
            Name = "unwatch",
            Description = "Discontinue posting updates to this channel",
            DefaultMemberPermissions = GuildPermission.ManageGuild,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new ()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                }
            }
        };

        private static SlashCommandBuilder errorBuilder = new()
        {
            Name = "error",
            Description = "Post bot errors to this channel",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>() { }
        };

        private static SlashCommandBuilder noerrorBuilder = new()
        {
            Name = "noerror",
            Description = "Dont post bot errors to this channel",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>() { }
        };

        private static SlashCommandBuilder updateBuilder = new()
        {
            Name = "update",
            Description = "Update trackable categories",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>() { }
        }; 
        
        private static SlashCommandBuilder forceBuilder = new()
        {
            Name = "force",
            Description = "Force bot to check for updates",
            DefaultMemberPermissions = GuildPermission.ManageGuild,
            Options = new List<SlashCommandOptionBuilder>() { }
        };

        private static SlashCommandBuilder blessBuilder = new()
        {
            Name = "manifest",
            Description = "Manifest a beta release",
            DefaultMemberPermissions = GuildPermission.SendMessages,
            Options = new List<SlashCommandOptionBuilder>() { }
        };
        
        private static SlashCommandBuilder goodBotBuilder = new()
        {
            Name = "goodbot",
            Description = "Tell the bot that it is doing a good job :)",
            DefaultMemberPermissions = GuildPermission.SendMessages,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new ()
                {
                    Name = "reason",
                    Description = "Something specific? Let me know!",
                    IsRequired = false,
                    Type = ApplicationCommandOptionType.String
                }
            }
        };
        
        private static SlashCommandBuilder badBotBuilder = new()
        {
            Name = "badbot",
            Description = "Something could be improved :(",
            DefaultMemberPermissions = GuildPermission.SendMessages,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new ()
                {
                    Name = "reason",
                    Description = "Describe your issue, developer will be notified.",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String
                }
            }
        };
        
        private static SlashCommandBuilder infoBuilder = new()
        {
            Name = "info",
            Description = "Which device is being used to check for updates",
            DefaultMemberPermissions = GuildPermission.SendMessages,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new ()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                }
            }
        };
        
        private static SlashCommandBuilder whenBuilder = new()
        {
            Name = "when",
            Description = "When is the next release scheduled? Answer could be helpful, maybe not, who knows.",
            DefaultMemberPermissions = GuildPermission.SendMessages,
            Options = new List<SlashCommandOptionBuilder>()
            { }
        };

        private static List<SlashCommandBuilder> CommandBuilders = new()
        {
            watchBuilder,
            unwatchBuilder,
            errorBuilder,
            noerrorBuilder,
            forceBuilder,
            updateBuilder,
            blessBuilder,
            goodBotBuilder,
            badBotBuilder,
            infoBuilder,
            // whenBuilder
        };

        #endregion
        #region commands
        public static void InitCommand(SocketSlashCommand command, DiscordRestClient? client)
        {
            using var db = new BetaContext();

            command.DeferAsync(ephemeral: true);

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
                command.FollowupAsync($"You will now receive {device.FriendlyName} updates in this channel.", ephemeral: true);
            }
            else
            {
                command.FollowupAsync($"You already receive {device!.FriendlyName} updates in this channel.", ephemeral: true);
            }
        }

        internal static void RemoveCommand(SocketSlashCommand command, DiscordRestClient? client)
        {
            using var db = new BetaContext();
            command.DeferAsync(ephemeral: true);

            var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.First().Value);
            var server = db.Servers.FirstOrDefault(s => s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device!.Category);
            var channel = client.GetChannelAsync(command.ChannelId!.Value).Result as RestTextChannel;
            var guild = client.GetGuildAsync(command.GuildId!.Value).Result;

            if (null == server)
            {
                command.FollowupAsync($"You were not receiving {device!.FriendlyName} updates in this channel", ephemeral: true);
            }
            else
            {
                db.Servers.Remove(server);
                db.SaveChanges();

                Logger.Info($"Removed notifications for {device!.FriendlyName} updates in {guild.Name}:{channel!.Name}");
                command.FollowupAsync($"You will no longer receive {device.FriendlyName} updates in this channel", ephemeral: true);
            }
        }

        internal static void ForceCommand(SocketSlashCommand command)
        {
            using var db = new BetaContext();
            command.DeferAsync(ephemeral: true);

            ApiSingleton.Instance.Timer_Elapsed(null, null!);

            Logger.Info($"Update forced by {command.User.GlobalName}");
            command.FollowupAsync("Updates checked.");
        }

        internal static void ErrorCommand(SocketSlashCommand arg, DiscordRestClient? restClient)
        {
            if (!IsAllowed(arg.User.Id))
            { 
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            }

            using var db = new BetaContext();

            var errorServer = db.ErrorServers.FirstOrDefault(s => s.ServerId == arg.GuildId!.Value && s.ChannelId == arg.ChannelId!.Value);
            if (null == errorServer)
            {
                db.ErrorServers.Add(new ErrorServer
                {
                    ChannelId = arg.ChannelId!.Value,
                    ServerId = arg.GuildId!.Value,
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

        internal static void RemoveErrorCommand(SocketSlashCommand arg)
        {
            if (!IsAllowed(arg.User.Id))
            {
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            }

            using var db = new BetaContext();

            var errorServer = db.ErrorServers.FirstOrDefault(s => s.ServerId == arg.GuildId!.Value && s.ChannelId == arg.ChannelId!.Value);

            if (null == errorServer)
            {
                arg.RespondAsync("bot errors were not set to go here", ephemeral: true);
            }
            else
            {
                db.ErrorServers.Remove(errorServer);

                arg.RespondAsync("errors will not post here anymore", ephemeral: true);

                db.SaveChanges();
            }
        }

        internal static void UpdateOptions(SocketSlashCommand arg, DiscordSocketClient bot)
        {
            if (!IsAllowed(arg.User.Id))
            {
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            }

            // watch + unwatch
            var commands = CommandBuilders.Where(c => c.Name.Contains("watch"));

            foreach (var c in commands)
            {
                c.Options.First(o => o.Name == "category").Choices = GetDeviceCategories();
            }

            var devices = CommandBuilders
                .First(c => c.Name == "watch").Options
                .First(c => c.Name == "category").Choices
                .Select(c => c.Name);
            

            Logger.Trace($"Devices to watch: {string.Join(" | ", devices)}");
            arg.DeferAsync(ephemeral: true);

            UpdateCommands(bot);

            arg.FollowupAsync("Reloaded commands.", ephemeral: true);
        }

        internal static void Manifest(SocketSlashCommand arg)
        {
            using var db = new BetaContext();
            var gifLocation = db.Configs.First(c => c.Name == "ManifestGif").Value;
            arg.RespondAsync(gifLocation);
        }

        internal static void GoodBot(SocketSlashCommand arg, DiscordRestClient bot)
        {
            arg.DeferAsync(ephemeral: true);
            
            using var db = new BetaContext();
            
            var reason = "";
            if (arg.Data.Options.Any())
            {
                reason = arg.Data.Options.First().Value.ToString();
                
                var embed = new EmbedBuilder
                {
                    Color = new Color(0,255,255),
                    Title = "Good Bot",
                    Description = $"User: {arg.User.Username}"
                };
                embed.AddField(name: "Reason", value: reason)
                    .AddField(name: "Server", value: bot.GetGuildAsync(arg.GuildId.Value).Result.Name)
                    .AddField(name: "Channel", value: ((RestTextChannel)bot.GetChannelAsync(arg.ChannelId.Value).Result).Name);

                foreach (var s in db.ErrorServers)
                {
                    var channel = bot.GetChannelAsync(s.ChannelId).Result as RestTextChannel;
                    channel.SendMessageAsync(embed: embed.Build());
                }
            }

            arg.FollowupAsync($"Thank you :)", ephemeral: true);
        }

        internal static void BadBot(SocketSlashCommand arg, DiscordRestClient bot)
        {
            arg.DeferAsync(ephemeral: true);
            
            using var db = new BetaContext();
            var reason = arg.Data.Options.First().Value.ToString();
            
            var embed = new EmbedBuilder
            {
                Color = new Color(255,0,0),
                Title = "Bad Bot",
                Description = $"User: {arg.User.Username}"
            };
            embed.AddField(name: "Reason", value: reason)
                .AddField(name: "Server", value: bot.GetGuildAsync(arg.GuildId.Value).Result.Name)
                .AddField(name: "Channel", value: ((RestTextChannel)bot.GetChannelAsync(arg.ChannelId.Value).Result).Name);

            foreach (var s in db.ErrorServers)
            {
                var channel = bot.GetChannelAsync(s.ChannelId).Result as RestTextChannel;
                channel.SendMessageAsync(embed: embed.Build());
            }
            
            arg.FollowupAsync($"Thank you for your feedback. A developer has been notified and may reach out.", ephemeral: true);
        }

        public static void DeviceInfo(SocketSlashCommand arg)
        {
            arg.DeferAsync(ephemeral: true);
            
            using var db = new BetaContext();
            var device = db.Devices.FirstOrDefault(d => d.Category == (string)arg.Data.Options.First().Value);
            var update = db.Updates
                .Where(u => u.Category == device.Category)
                .OrderByDescending(u => u.ReleaseDate)
                .FirstOrDefault();
            
            var embed = new EmbedBuilder
            {
                Color = new Color(device.Color),
                Title = "Device Info",
                Description = $"{device.FriendlyName} feed"
            };
            embed.AddField(name: "Device", value: device.Name)
                .AddField(name: "Device Version", value: $"{device.Version} ({device.BuildId})")
                .AddField(name: "Newest Version", value: $"{update.Version} ({update.Build})");
            
            arg.FollowupAsync(embed: embed.Build());
        }
        
        public static void When(SocketSlashCommand arg)
        {
            arg.DeferAsync(ephemeral: true);

            var responses = new string[]
            {
                "Son (tm)",
                "useful",
                "Release time was just pushed back 5 more minutes",
                "Tim Apple said maybe next week",
                "useful",
                "There isn't one, the next beta is the friends we made along the way",
                "useful",
                "useful",
                "Many moons from now",
                "Eventually",
                "useful",
                "I think Tim hit the snooze button on his alarm",
                "I heard that Tim’s dog ate the Beta",
                "useful",
                "Once AirPower is released",
                "useful"
            };

            string resp = responses[new Random().Next(responses.Length)];

            if (resp == "useful")
            {
                resp = GetUsefulResponse();
            }

            arg.FollowupAsync(resp, ephemeral: true);
        }

        #endregion
        #region helpers

        private static List<ApplicationCommandOptionChoiceProperties> GetDeviceCategories()
        {
            var devices = GetDevices();

            return devices.Select(c => new ApplicationCommandOptionChoiceProperties { Name = c.FriendlyName, Value = c.Category }).ToList();
        }

        public static void UpdateCommands(DiscordSocketClient client)
        {
            try
            {
                Logger.Info($"Updating commands");
                Logger.Trace(string.Join(" | ", CommandBuilders.Select(c => c.Name)));
                client.BulkOverwriteGlobalApplicationCommandsAsync(
                    CommandBuilders.Select(b => b.Build()).ToArray());
            }
            catch (HttpException e)
            {
                var json = JsonConvert.SerializeObject(e.Reason, Formatting.Indented);
                Logger.Error(new LogMessage(LogSeverity.Error, "RegisterCommand", json, e));
            }
        }

        private static bool IsAllowed(ulong userId)
        {
            // only me
            return userId == 191051620430249984;
        }

        private static List<Device> GetDevices()
        {
            using var db = new BetaContext();

            return db.Devices.ToList();
        }

        private static string GetUsefulResponse()
        {
            using var db = new BetaContext();

            var current = db.Releases.OrderByDescending(r => r.Date).First();

            var allMinorWithBeta = db.Releases.Where(r => r.Beta == current.Beta && r.Minor == current.Minor);
            
            int daysSinceRelease = new TimeSpan(DateTime.Today.Millisecond - current.Date.Millisecond).Days;
            int averageWait;
            
            if (allMinorWithBeta.Any())
            {
                averageWait = (int)Math.Round(allMinorWithBeta.Average(r => r.WaitTime));
            }
            else
            {
                averageWait = (int)Math.Round(db.Releases
                                                .Where(r => r.Major == current.Major && r.Minor == current.Minor)
                                                .Average(r => r.WaitTime));
            }

            return $"Based on previous releases, the next release is expected in {averageWait} days.";
        }

        public static void PostError(DiscordSocketClient bot, IAppleService appleService, string message)
        {
            try
            {
                using var db = new BetaContext();

                foreach (var s in db.ErrorServers)
                {
                    var server = (SocketTextChannel)bot.GetChannelAsync(s.ChannelId).Result;
                    if (null == server)
                    {
                        appleService.DeleteErrorServer(s, db);
                        continue;
                    }
                    if (!message.EndsWith("Server requested a reconnect") &&
                        !message.EndsWith("WebSocket connection was closed"))
                    {
                        
                        server.SendMessageAsync(message);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Environment.Exit(1);
            }
        }
        
        #endregion
    }
}
