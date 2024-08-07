﻿using System.Text;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Bot.Singletons;
using iOSBot.Data;
using iOSBot.Service;
using Newtonsoft.Json;
using NLog;
using Thread = iOSBot.Data.Thread;
using Update = iOSBot.Service.Update;

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
                new()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                },
                new()
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
                new()
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

        private static SlashCommandBuilder whyCraigBuilder = new()
        {
            Name = "whycraig",
            Description = "???",
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
                new()
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
                new()
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
                new()
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

        private static SlashCommandBuilder serverBuilder = new()
        {
            Name = "servers",
            Description = "See servers that this bot is in",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>() { }
        };

        private static SlashCommandBuilder gambaBuilder = new()
        {
            Name = "gamba",
            Description = "Start a gamba",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new SlashCommandOptionBuilder()
                {
                    Name = "Prompt",
                    Description = "Prediction Prompt",
                    Type = ApplicationCommandOptionType.String,
                    IsRequired = true
                }
            }
        };

        private static SlashCommandBuilder stopBuilder = new()
        {
            Name = "stop",
            Description = "Stop update checks",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>() { }
        };

        private static SlashCommandBuilder startBuilder = new()
        {
            Name = "start",
            Description = "start update checks",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>() { }
        };

        private static SlashCommandBuilder statusBuilder = new()
        {
            Name = "status",
            Description = "update check timer status",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>() { }
        };

        private static SlashCommandBuilder newThreadBuilder = new()
        {
            Name = "yesthreads",
            Description = "Create a release thread in this channel when an update is released",
            DefaultMemberPermissions = GuildPermission.ManageGuild,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                }
            }
        };

        private static SlashCommandBuilder deleteThreadBuilder = new()
        {
            Name = "nothreads",
            Description = "Discontinue release thread auto creation in this channel when an update is released",
            DefaultMemberPermissions = GuildPermission.ManageGuild,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                }
            }
        };

        private static SlashCommandBuilder yesForumBuilder = new()
        {
            Name = "yesforum",
            Description = "Create a forum thread in specified channel",
            DefaultMemberPermissions = GuildPermission.ManageGuild,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                },
                new()
                {
                    Name = "channel",
                    Description = "Id of the forum channel to post in",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.Channel
                }
            }
        };

        private static SlashCommandBuilder noForumBuilder = new()
        {
            Name = "noforum",
            Description = "Do not create a forum thread in specified channel",
            DefaultMemberPermissions = GuildPermission.ManageGuild,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                },
                new()
                {
                    Name = "channel",
                    Description = "Id of the forum channel to post in",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.Channel
                }
            }
        };

        private static SlashCommandBuilder fakePostBuilder = new()
        {
            Name = "fake",
            Description = "Send a fake notification",
            DefaultMemberPermissions = GuildPermission.Administrator,
            Options = new List<SlashCommandOptionBuilder>()
            {
                new()
                {
                    Name = "category",
                    Description = "Which OS updates",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                    Choices = GetDeviceCategories()
                },
                new()
                {
                    Name = "build",
                    Description = "Build ID of the update",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                },
                new()
                {
                    Name = "version",
                    Description = "Build version of the update",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                },
                new()
                {
                    Name = "docid",
                    Description = "documentationid for update. 18Beta0, 173Long, 1934Beta2, etc",
                    IsRequired = true,
                    Type = ApplicationCommandOptionType.String,
                },
            }
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
            serverBuilder,
            whenBuilder,
            // gambaBuilder,
            statusBuilder,
            startBuilder,
            stopBuilder,
            newThreadBuilder,
            deleteThreadBuilder,
            yesForumBuilder,
            noForumBuilder,
            fakePostBuilder,
            whyCraigBuilder
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

            var server = db.Servers.FirstOrDefault(s =>
                s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device!.Category);

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
                command.FollowupAsync($"You will now receive {device.FriendlyName} updates in this channel.",
                    ephemeral: true);
            }
            else
            {
                command.FollowupAsync($"You already receive {device!.FriendlyName} updates in this channel.",
                    ephemeral: true);
            }
        }

        internal static void RemoveCommand(SocketSlashCommand command, DiscordRestClient? client)
        {
            using var db = new BetaContext();
            command.DeferAsync(ephemeral: true);

            var device = db.Devices.FirstOrDefault(d => d.Category == (string)command.Data.Options.First().Value);
            var server = db.Servers.FirstOrDefault(s =>
                s.ChannelId == command.ChannelId && s.ServerId == command.GuildId && s.Category == device!.Category);
            var channel = client.GetChannelAsync(command.ChannelId!.Value).Result as RestTextChannel;
            var guild = client.GetGuildAsync(command.GuildId!.Value).Result;

            if (null == server)
            {
                command.FollowupAsync($"You were not receiving {device!.FriendlyName} updates in this channel",
                    ephemeral: true);
            }
            else
            {
                db.Servers.Remove(server);
                db.SaveChanges();

                Logger.Info(
                    $"Removed notifications for {device!.FriendlyName} updates in {guild.Name}:{channel!.Name}");
                command.FollowupAsync($"You will no longer receive {device.FriendlyName} updates in this channel",
                    ephemeral: true);
            }
        }

        internal static void ForceCommand(SocketSlashCommand command)
        {
            if (!IsAllowed(command.User.Id))
            {
                command.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
            }

            // try to prevent what looks like some race conditions
            ApiSingleton.Instance.StopTimer();

            command.DeferAsync(ephemeral: true);

            ApiSingleton.Instance.Timer_Elapsed(null, null!);

            ApiSingleton.Instance.StartTimer();

            Logger.Info($"Update forced by {command.User.GlobalName}");
            command.FollowupAsync("Updates checked.");
        }

        internal static void ErrorCommand(SocketSlashCommand arg, DiscordRestClient? restClient)
        {
            if (!IsAllowed(arg.User.Id))
            {
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                return;
            }

            using var db = new BetaContext();

            var errorServer = db.ErrorServers.FirstOrDefault(s =>
                s.ServerId == arg.GuildId!.Value && s.ChannelId == arg.ChannelId!.Value);
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
                arg.RespondAsync("Errors are already set to be posted here.", ephemeral: true);
            }
        }

        internal static void RemoveErrorCommand(SocketSlashCommand arg)
        {
            if (!IsAllowed(arg.User.Id))
            {
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                return;
            }

            using var db = new BetaContext();

            var errorServer = db.ErrorServers.FirstOrDefault(s =>
                s.ServerId == arg.GuildId!.Value && s.ChannelId == arg.ChannelId!.Value);

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
                return;
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

        internal static void WhyCraig(SocketSlashCommand arg)
        {
            using var db = new BetaContext();
            var imgSrc = db.Configs.First(c => c.Name == "WhyCraig").Value;
            arg.RespondAsync(imgSrc);
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
                    Color = new Color(0, 255, 255),
                    Title = "Good Bot",
                    Description = $"User: {arg.User.Username}"
                };
                embed.AddField(name: "Reason", value: reason)
                    .AddField(name: "Server", value: bot.GetGuildAsync(arg.GuildId.Value).Result.Name)
                    .AddField(name: "Channel",
                        value: ((RestTextChannel)bot.GetChannelAsync(arg.ChannelId.Value).Result).Name);

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
                Color = new Color(255, 0, 0),
                Title = "Bad Bot",
                Description = $"User: {arg.User.Username}"
            };
            embed.AddField(name: "Reason", value: reason)
                .AddField(name: "Server", value: bot.GetGuildAsync(arg.GuildId.Value).Result.Name)
                .AddField(name: "Channel",
                    value: ((RestTextChannel)bot.GetChannelAsync(arg.ChannelId.Value).Result).Name);

            foreach (var s in db.ErrorServers)
            {
                var channel = bot.GetChannelAsync(s.ChannelId).Result as RestTextChannel;
                channel.SendMessageAsync(embed: embed.Build());
            }

            arg.FollowupAsync($"Thank you for your feedback. A developer has been notified and may reach out.",
                ephemeral: true);
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
            arg.DeferAsync();

            var rand = new Random();

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

            var resp = responses[rand.Next(responses.Length)];

            if (resp == "useful")
            {
                if (rand.NextDouble() > 0.5) resp = "https://www.thinkybits.com/blog/iOS-versions/";
                else
                {
                    // lmfao
                    var now = DateTime.Now;
                    var today1pm = DateTime.Today.AddHours(13);
                    var today4pm = DateTime.Today.AddHours(16);
                    if (now > today1pm && now < today4pm)
                    {
                        resp = "Could be any minute now";
                    }
                    else
                    {
                        if (now > today4pm) today1pm = today1pm.AddDays(1);
                        var offset = DateTimeOffset.Parse(today1pm.ToLongDateString()).AddHours(13);

                        if (offset.DayOfWeek == DayOfWeek.Saturday) offset = offset.AddDays(2);
                        else if (offset.DayOfWeek == DayOfWeek.Sunday) offset = offset.AddDays(1);

                        resp = $"*Possibly* in <t:{offset.ToUnixTimeSeconds()}:R>";
                    }
                }
            }

            arg.FollowupAsync(resp);
        }

        public static void GetServers(SocketSlashCommand arg, DiscordRestClient bot)
        {
            if (!IsAllowed(arg.User.Id))
            {
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                return;
            }

            arg.DeferAsync(ephemeral: true);
            var servers = bot.GetGuildsAsync().Result.ToArray();
            StringBuilder response = new StringBuilder();
            response.AppendLine("Servers:");

            for (int i = 0; i < servers.Length; ++i)
            {
                response.AppendLine($"{i + 1}: {servers[i].Name} (@{servers[i].GetOwnerAsync().Result})");
            }

            arg.FollowupAsync(response.ToString());
        }

        public static void BotStatus(SocketSlashCommand arg, StatusCommand command)
        {
            if (command != StatusCommand.STATUS)
            {
                if (!IsAllowed(arg.User.Id))
                {
                    arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                    return;
                }
            }

            arg.DeferAsync(ephemeral: true);

            switch (command)
            {
                case StatusCommand.STATUS:
                    string response = ApiSingleton.Instance.IsRunning ? "Running" : "Not Running";
                    arg.FollowupAsync(ephemeral: true, text: $"Bot is currently: {response}");
                    break;
                case StatusCommand.START:
                    ApiSingleton.Instance.StartTimer();
                    arg.FollowupAsync(ephemeral: true, text: $"Bot is running");
                    break;
                case StatusCommand.STOP:
                    ApiSingleton.Instance.StopTimer();
                    arg.FollowupAsync(ephemeral: true, text: $"Bot is stopped");
                    break;
            }
        }

        public static void NewThreadChannel(SocketSlashCommand arg)
        {
            arg.DeferAsync(ephemeral: true);

            using var db = new BetaContext();
            var category = (string)arg.Data.Options.First().Value;

            db.Threads.Add(new Thread()
            {
                Category = category,
                ChannelId = arg.ChannelId.Value,
                ServerId = arg.GuildId.Value,
                id = Guid.NewGuid()
            });

            db.SaveChanges();

            arg.FollowupAsync(text: "A release thread will be posted here.", ephemeral: true);
        }

        public static void DeleteThreadChannel(SocketSlashCommand arg)
        {
            arg.DeferAsync(ephemeral: true);
            var category = (string)arg.Data.Options.First().Value;

            using var db = new BetaContext();

            var thread = db.Threads.FirstOrDefault(t => t.ChannelId == arg.ChannelId && t.Category == category);

            if (thread is null)
            {
                arg.FollowupAsync(text: "Release threads were not set to be posted here", ephemeral: true);
                return;
            }

            db.Threads.Remove(thread);

            db.SaveChanges();

            arg.FollowupAsync(text: "Release threads will no longer be posted here.", ephemeral: true);
        }

        public static void FakeUpdate(SocketSlashCommand arg)
        {
            if (!IsAllowed(arg.User.Id))
            {
                arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                return;
            }

            arg.DeferAsync();

            var category = (string)arg.Data.Options.First(o => o.Name == "category").Value;
            var fakeBuild = (string)arg.Data.Options.First(o => o.Name == "build").Value;
            var fakeVersion = (string)arg.Data.Options.First(o => o.Name == "version").Value;
            var fakeDocId = (string)arg.Data.Options.First(o => o.Name == "docid").Value;

            var db = new BetaContext();

            var device = db.Devices.FirstOrDefault(d => d.Category == category);
            var servers = db.Servers.Where(s => s.Category == category);

            var fakeUpdate = new Update()
            {
                Build = fakeBuild,
                Device = device,
                Version = fakeVersion,
                SizeBytes = 69420000000,
                ReleaseDate = DateTime.Today,
                ReleaseType = device.Type,
                VersionDocId = fakeDocId,
                Group = device.Category
            };

            foreach (var s in servers)
            {
                ApiSingleton.Instance.SendAlert(fakeUpdate, s);
            }

            arg.FollowupAsync("Posted update", ephemeral: true);
        }

        public static void AddForumPost(SocketSlashCommand arg, DiscordRestClient bot)
        {
            arg.DeferAsync(ephemeral: true);

            var channel = arg.Data.Options.First(o => o.Name == "channel").Value;
            var category = arg.Data.Options.First(o => o.Name == "category").Value.ToString();

            if (null == channel || channel.GetType() != typeof(SocketForumChannel))
            {
                arg.FollowupAsync("Please select a forum channel I can write to", ephemeral: true);
                return;
            }

            var forum = channel as SocketForumChannel ?? throw new Exception("Channel is not a forum channel");

            using var db = new BetaContext();
            var dbF = db.Forums.FirstOrDefault(f => f.Category == category && f.ChannelId == forum.Id);
            if (null != dbF)
            {
                arg.FollowupAsync($"Forum posts will already happen for {category} in {forum.Name}", ephemeral: true);
                return;
            }

            db.Forums.Add(new Forum()
            {
                ChannelId = forum.Id,
                Category = category,
                ServerId = forum.Guild.Id,
                id = Guid.NewGuid()
            });

            db.SaveChangesAsync();
            arg.FollowupAsync("Forum posts will happen here", ephemeral: true);
        }

        public static void RemoveForumPost(SocketSlashCommand arg, DiscordRestClient bot)
        {
            arg.DeferAsync(ephemeral: true);

            var channel = arg.Data.Options.First(o => o.Name == "channel").Value;
            var category = arg.Data.Options.First(o => o.Name == "category").Value.ToString();

            if (null == channel || channel.GetType() != typeof(SocketForumChannel))
            {
                arg.FollowupAsync("Please select a forum channel I can write to", ephemeral: true);
                return;
            }

            var forum = channel as SocketForumChannel ?? throw new Exception("Channel is not a forum channel");

            using var db = new BetaContext();
            var dbF = db.Forums.FirstOrDefault(f => f.Category == category && f.ChannelId == forum.Id);
            if (null == dbF)
            {
                arg.FollowupAsync($"Forum posts are not set to happen for {category} in {forum.Name}", ephemeral: true);
                return;
            }

            db.Forums.Remove(dbF);
            db.SaveChanges();
            arg.FollowupAsync($"Forum posts for {category} will no longer happen in ${forum.Name}", ephemeral: true);
        }

        #endregion

        #region helpers

        private static List<ApplicationCommandOptionChoiceProperties> GetDeviceCategories()
        {
            var devices = GetDevices();

            return devices.Select(c => new ApplicationCommandOptionChoiceProperties
                { Name = c.FriendlyName, Value = c.Category }).ToList();
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

        public static void PostError(DiscordSocketClient bot, IAppleService appleService, string message)
        {
            try
            {
                using var db = new BetaContext();

                foreach (var s in db.ErrorServers)
                {
                    IChannel server = bot.GetChannelAsync(s.ChannelId).Result;
                    if (null == server)
                    {
                        appleService.DeleteErrorServer(s, db);
                        continue;
                    }

                    if (!message.EndsWith("Server requested a reconnect") &&
                        !message.EndsWith("WebSocket connection was closed"))
                    {
                        ((ITextChannel)server).SendMessageAsync(message);
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

    public enum StatusCommand
    {
        START,
        STOP,
        STATUS
    }
}