using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using NLog;

namespace iOSBot.Bot.Commands;

public class MemeCommands
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            .AddField(name: "Channel", value: ((RestTextChannel)bot.GetChannelAsync(arg.ChannelId.Value).Result).Name);

        foreach (var s in db.ErrorServers)
        {
            var channel = bot.GetChannelAsync(s.ChannelId).Result as RestTextChannel;
            channel.SendMessageAsync(embed: embed.Build());
        }

        arg.FollowupAsync($"Thank you for your feedback. A developer has been notified and may reach out.",
            ephemeral: true);
    }
}