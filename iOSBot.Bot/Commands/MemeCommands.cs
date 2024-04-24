using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;

namespace iOSBot.Bot.Commands;

public class MemeCommands
{
/*
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
*/

    internal static async void Manifest(SocketSlashCommand arg)
    {
        using var db = new BetaContext();
        var gifLocation = db.Configs.First(c => c.Name == "ManifestGif").Value;
        await arg.RespondAsync(gifLocation);
    }

    internal static async void GoodBot(SocketSlashCommand arg, DiscordRestClient bot)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();

        string reason;
        if (arg.Data.Options.Any())
        {
            // cant be null
            reason = arg.Data.Options.First().Value.ToString()!;

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
                await channel.SendMessageAsync(embed: embed.Build());
            }
        }

        await arg.FollowupAsync($"Thank you :)", ephemeral: true);
    }

    internal static async void BadBot(SocketSlashCommand arg, DiscordRestClient bot)
    {
        await arg.DeferAsync(ephemeral: true);

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
            var channel = bot.GetChannelAsync(s.ChannelId).Result as RestTextChannel ?? throw new Exception("Cant find channel");
            await channel.SendMessageAsync(embed: embed.Build());
        }

        await arg.FollowupAsync($"Thank you for your feedback. A developer has been notified and may reach out.",
            ephemeral: true);
    }
}