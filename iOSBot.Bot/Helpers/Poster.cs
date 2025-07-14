using Discord;
using Discord.WebSocket;
using iOSBot.Data;
using iOSBot.Service;
using Microsoft.Extensions.Logging;
using Thread = iOSBot.Data.Thread;

namespace iOSBot.Bot.Helpers;

public class Poster(DiscordSocketClient client, ILogger<Poster> logger)
{
    private DiscordSocketClient Client { get; init; } = client;

    public static async void StaticError(Poster poster, string message)
        => await poster.PostError(message);

    public async Task PostError(string message)
    {
        try
        {
            await using var db = new BetaContext();

            foreach (var s in db.ErrorServers)
            {
                var server = await Client.GetChannelAsync(s.ChannelId);
                if (null == server)
                {
                    logger.LogError("Cannot get error channel {SChannelId}", s.ChannelId);
                    continue;
                }

                if (!message.EndsWith("Server requested a reconnect") &&
                    !message.EndsWith("WebSocket connection was closed"))
                {
                    await ((ITextChannel)server).SendMessageAsync(message);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(1, e, "Error in poster. {error}", e.Message);
        }
    }

    public async Task<IThreadChannel?> CreateThreadAsync(Thread thread, MqUpdate update)
    {
        var channel = await Client.GetChannelAsync(thread.ChannelId) as ITextChannel;
        if (channel == null)
        {
            logger.LogError("Channel {ThreadChannelId} does not exist", thread.ChannelId);
            return null;
        }

        logger.LogInformation($"Creating thread in {channel.Name} for {update.Version}");
        return await channel.CreateThreadAsync($"{update.Version} Release Thread");
    }

    public async Task<IThreadChannel?> CreateForumAsync(Forum forum, MqUpdate update)
    {
        var dForum = await Client.GetChannelAsync(forum.ChannelId) as IForumChannel;
        if (null == dForum)
        {
            logger.LogError("forum {ForumChannelId} does not exist", forum.ChannelId);
            return null;
        }

        logger.LogInformation($"Creating post in {dForum.Name} for {update.Version}");
        var embed = new EmbedBuilder()
        {
            Color = update.Color,
            Title = update.Version,
        };
        embed.AddField(name: "Build", value: update.Build)
            .AddField(name: "Size", value: update.Size)
            .AddField(name: "Release Date", value: update.ReleaseDate.ToShortDateString());

        var imagePath = GetImagePath(update.TrackName);
        if (!string.IsNullOrEmpty(imagePath))
        {
            embed.ThumbnailUrl = imagePath;
        }

        return await dForum.CreatePostAsync(title: $"{update.Version} Discussion",
            text: $"Discuss the release of {update.Version} here.",
            embed: embed.Build(),
            archiveDuration: ThreadArchiveDuration.OneWeek);
    }

    public async Task<IUserMessage> PostUpdateAsync(Server server, MqUpdate update, List<string>? postedThreads = null,
        List<string>? postedForums = null)
    {
        var channel = await Client.GetChannelAsync(server.ChannelId) as ITextChannel;
        if (null == channel)
        {
            logger.LogWarning("Channel with id {ServerChannelId} doesnt exist.", server.ChannelId);

            return null!;
        }

        var mention = server.TagId != "" ? $"<@&{server.TagId}>" : "";

        var embed = new EmbedBuilder
        {
            Color = new Color(update.Color),
            Title = $"New {update.TrackName} Release!",
            Timestamp = DateTime.Now,
        };
        embed.AddField(name: "Version", value: update.Version)
            .AddField(name: "Build", value: update.Build)
            .AddField(name: "Size", value: update.Size);

        if (null != postedThreads && postedThreads.Any())
            embed.AddField(name: "Discussion Thread(s)", value: string.Join('\n', postedThreads));
        if (null != postedForums && postedForums.Any())
            embed.AddField(name: "Discussion Forum(s)", value: string.Join('\n', postedForums));
        var isGM = update.Version.Contains("Golden Master"); // split because testing
        if (isGM)
            embed.AddField(name: "Why Golden Master?", value: "use /whygm to learn why.");


        var imagePath = GetImagePath(update.TrackName);
        if (!string.IsNullOrEmpty(imagePath))
        {
            embed.ThumbnailUrl = imagePath;
        }

        logger.LogInformation("Posting {UpdateVersion} to {ChannelName}", update.Version, channel.Name);
        try
        {
            return await channel.SendMessageAsync(text: mention, embed: embed.Build());
        }
        catch (Exception ex)
        {
            logger.LogError(420, ex, "Error posting to {ChannelName}. {ErrorMessage}", channel.Name, ex.Message);
            ;
            await PostError($"Error posting to {channel.Name}. {ex.Message}");
        }

        return null!;
    }

    private string GetImagePath(string category)
    {
        if (category.Contains("ios", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/iphone.png";
        if (category.Contains("mac", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/mac.png";
        if (category.Contains("tv", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/tv.png";
        if (category.Contains("watch", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/watch.png";
        if (category.Contains("vision", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/vision.png";

        return string.Empty;
    }
}