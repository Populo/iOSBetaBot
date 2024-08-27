using Discord;
using Discord.WebSocket;
using iOSBot.Bot.Helpers;
using iOSBot.Data;

namespace iOSBot.Bot.Commands;

public class MemeCommands
{
    public static async Task Manifest(SocketSlashCommand arg)
    {
        using var db = new BetaContext();
        var rand = new Random();
        var configItem = rand.Next(100) > 50 ? "ManifestGif" : "PlsCraigGif";

        var gifLocation = db.Configs.First(c => c.Name == configItem).Value;
        await arg.RespondAsync(gifLocation);
    }

    public static async Task WhyCraig(SocketSlashCommand arg)
    {
        using var db = new BetaContext();
        var imgSrc = db.Configs.First(c => c.Name == "WhyCraig").Value;
        await arg.RespondAsync(imgSrc);
    }

    public static async Task GoodBot(SocketSlashCommand arg, DiscordSocketClient bot)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();

        string reason;
        if (arg.Data.Options.Any())
        {
            reason = arg.Data.Options.First().Value as string
                     ?? throw new Exception("Cannot get reason");
            SocketTextChannel channel;
            SocketGuild guild;
            CommandObjects.GetChannelAndGuild(arg, bot, out guild, out channel);

            var embed = new EmbedBuilder
            {
                Color = new Color(0, 255, 255),
                Title = "Good Bot",
                Description = $"User: {arg.User.Username}"
            };
            embed.AddField(name: "Reason", value: reason)
                .AddField(name: "Server", value: guild.Name)
                .AddField(name: "Channel", channel.Name);

            foreach (var s in db.ErrorServers)
            {
                var errorChannel = await bot.GetChannelAsync(s.ChannelId) as SocketTextChannel
                                   ?? throw new Exception("Cannot get error channel");
                await errorChannel.SendMessageAsync(embed: embed.Build());
            }
        }

        await arg.FollowupAsync($"Thank you :)", ephemeral: true);
    }

    public static async Task BadBot(SocketSlashCommand arg, DiscordSocketClient bot)
    {
        await arg.DeferAsync(ephemeral: true);

        using var db = new BetaContext();
        var reason = arg.Data.Options.First().Value.ToString();

        SocketTextChannel channel;
        SocketGuild guild;
        CommandObjects.GetChannelAndGuild(arg, bot, out guild, out channel);

        var embed = new EmbedBuilder
        {
            Color = new Color(255, 0, 0),
            Title = "Bad Bot",
            Description = $"User: {arg.User.Username}"
        };
        embed.AddField(name: "Reason", value: reason)
            .AddField(name: "Server", value: guild.Name)
            .AddField(name: "Channel", channel.Name);

        foreach (var s in db.ErrorServers)
        {
            var errorChannel = await bot.GetChannelAsync(s.ChannelId) as SocketTextChannel
                               ?? throw new Exception("Cannot get error channel");
            await errorChannel.SendMessageAsync(embed: embed.Build());
        }

        await arg.FollowupAsync($"Thank you for your feedback. A developer has been notified and may reach out.",
            ephemeral: true);
    }

    // TODO react to any message in channel with word
    public static async Task YesReact(SocketSlashCommand arg)
    {
    }

    // TODO undo YesReact
    public static async Task NoReact(SocketSlashCommand arg)
    {
    }

    public static async Task When(SocketSlashCommand arg)
    {
        await arg.DeferAsync();

        var rand = new Random();

        var responses = new[]
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

                    resp = $"*Possibly* <t:{offset.ToUnixTimeSeconds()}:R>";
                }
            }
        }

        await arg.FollowupAsync(resp);
    }
}