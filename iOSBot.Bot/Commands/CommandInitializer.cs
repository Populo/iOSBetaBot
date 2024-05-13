using Discord;
using Discord.Net;
using Discord.WebSocket;
using iOSBot.Data;
using Newtonsoft.Json;
using NLog;

namespace iOSBot.Bot.Commands;

public static class CommandInitializer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        Name = "yeserror",
        Description = "Post bot errors to this channel",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>()
    };

    private static SlashCommandBuilder noerrorBuilder = new()
    {
        Name = "noerror",
        Description = "Dont post bot errors to this channel",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>()
    };

    private static SlashCommandBuilder updateBuilder = new()
    {
        Name = "update",
        Description = "Update trackable categories",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>()
    };

    private static SlashCommandBuilder forceBuilder = new()
    {
        Name = "force",
        Description = "Force bot to check for updates",
        DefaultMemberPermissions = GuildPermission.ManageGuild,
        Options = new List<SlashCommandOptionBuilder>()
    };

    private static SlashCommandBuilder blessBuilder = new()
    {
        Name = "manifest",
        Description = "Manifest a beta release",
        DefaultMemberPermissions = GuildPermission.SendMessages,
        Options = new List<SlashCommandOptionBuilder>()
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

    private static SlashCommandBuilder serverBuilder = new()
    {
        Name = "servers",
        Description = "See servers that this bot is in",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>()
    };

/*
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
*/

    private static SlashCommandBuilder stopBuilder = new()
    {
        Name = "stop",
        Description = "Stop update checks",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>()
    };

    private static SlashCommandBuilder startBuilder = new()
    {
        Name = "start",
        Description = "start update checks",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>()
    };

    private static SlashCommandBuilder statusBuilder = new()
    {
        Name = "status",
        Description = "update check timer status",
        DefaultMemberPermissions = GuildPermission.Administrator,
        Options = new List<SlashCommandOptionBuilder>()
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

    public static readonly List<SlashCommandBuilder> CommandBuilders =
    [
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
        // gambaBuilder,
        statusBuilder,
        startBuilder,
        stopBuilder,
        newThreadBuilder,
        deleteThreadBuilder,
        fakePostBuilder
    ];
    
    public static List<ApplicationCommandOptionChoiceProperties> GetDeviceCategories()
    {
        var devices = GetDevices();

        return devices.Select(c => new ApplicationCommandOptionChoiceProperties { Name = c.FriendlyName, Value = c.Category }).ToList();
    }
    
    private static List<Device> GetDevices()
    {
        using var db = new BetaContext();

        return db.Devices.ToList();
    }
    
    public static async void UpdateCommands(DiscordSocketClient client)
    {
        try
        {
            Logger.Info($"Updating commands");
            Logger.Trace(string.Join(" | ", CommandBuilders.Select(c => c.Name)));
            var commands = CommandBuilders.Select(b => b.Build() as ApplicationCommandProperties).ToArray() ??
                           throw new Exception("Cannot init commands");
            await client.BulkOverwriteGlobalApplicationCommandsAsync(commands);
        }
        catch (HttpException e)
        {
            var json = JsonConvert.SerializeObject(e.Reason, Formatting.Indented);
            Logger.Error(new LogMessage(LogSeverity.Error, "RegisterCommand", json, e));
        }
    }
}