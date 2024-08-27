using Discord;
using Discord.WebSocket;
using iOSBot.Data;

namespace iOSBot.Bot.Helpers;

public class CommandObjects
{
    #region Builers

    private static SlashCommandBuilder errorBuilder = new()
    {
        Name = "yeserrors",
        Description = "Post bot errors to this channel",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>() { }
    };

    private static SlashCommandBuilder noerrorBuilder = new()
    {
        Name = "noerrors",
        Description = "Dont post bot errors to this channel",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>() { }
    };

    private static SlashCommandBuilder updateBuilder = new()
    {
        Name = "update",
        Description = "Update bot commands",
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

    private static SlashCommandBuilder serverBuilder = new()
    {
        Name = "servers",
        Description = "See servers that this bot is in",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>() { }
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

    private static SlashCommandBuilder toggleDeviceBuilder = new()
    {
        Name = "toggle",
        Description = "Enable or disable devices",
        DefaultMemberPermissions = GuildPermission.ManageGuild,
        Options = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "category",
                Description = "Which devices",
                IsRequired = true,
                Type = ApplicationCommandOptionType.String,
                Choices = GetDeviceCategories()
            },
            new()
            {
                Name = "enable",
                Description = "Enable updates",
                IsRequired = true,
                Type = ApplicationCommandOptionType.Boolean,
            }
        }
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

    private static SlashCommandBuilder whenBuilder = new()
    {
        Name = "when",
        Description = "When is the next release scheduled? Answer could be helpful, maybe not, who knows.",
        DefaultMemberPermissions = GuildPermission.SendMessages,
        Options = new List<SlashCommandOptionBuilder>()
            { }
    };

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

    private static SlashCommandBuilder yesThreadBuilder = new()
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

    private static SlashCommandBuilder noThreadBuilder = new()
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

    public static List<SlashCommandBuilder> CommandBuilders = new()
    {
        // admin commands
        errorBuilder,
        noerrorBuilder,
        forceBuilder,
        updateBuilder,
        serverBuilder,
        startBuilder,
        stopBuilder,
        fakePostBuilder,
        //toggleDeviceBuilder,
        // meme commands
        blessBuilder,
        goodBotBuilder,
        badBotBuilder,
        whenBuilder,
        whyCraigBuilder,
        // apple commands
        infoBuilder,
        watchBuilder,
        unwatchBuilder,
        yesThreadBuilder,
        noThreadBuilder,
        yesForumBuilder,
        noForumBuilder
    };

    #endregion

    #region admin commands

    #endregion

    #region meme commands

    #endregion

    #region apple commands

    #endregion

    #region Helpers

    private static List<ApplicationCommandOptionChoiceProperties> GetDeviceCategories()
    {
        var devices = GetDevices();

        return devices.Select(c => new ApplicationCommandOptionChoiceProperties
            { Name = c.FriendlyName, Value = c.Category }).ToList();
    }

    private static List<Device> GetDevices()
    {
        using var db = new BetaContext();

        return db.Devices
            .GroupBy(d => d.Category)
            .SelectMany(d => d)
            .ToList();
    }

    public static void GetChannelAndGuild(SocketSlashCommand command, DiscordSocketClient bot, out SocketGuild guild,
        out SocketTextChannel channel)
    {
        var cId = command.ChannelId
                  ?? throw new Exception("Cannot get channelId");
        var gId = command.GuildId
                  ?? throw new Exception("Cannot get guildId");

        channel = bot.GetChannelAsync(cId).GetAwaiter().GetResult() as SocketTextChannel
                  ?? throw new Exception("Could not get channel");
        guild = bot.GetGuild(gId)
                ?? throw new Exception("Could not get guild");
    }

    #endregion
}