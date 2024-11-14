using Discord;
using Discord.WebSocket;
using iOSBot.Data;
using iOSBot.Service;
using NLog;
using Thread = iOSBot.Data.Thread;
using Update = iOSBot.Service.Update;

namespace iOSBot.Bot.Helpers;

public class Poster
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Poster(IAppleService appleService, DiscordSocketClient client)
    {
        AppleService = appleService;
        Client = client;
    }

    private IAppleService AppleService { get; init; }
    private DiscordSocketClient Client { get; init; }

    public static async void StaticError(Poster poster, string message)
        => await poster.PostError(message);

    public async Task PostError(string message)
    {
        try
        {
            using var db = new BetaContext();

            foreach (var s in db.ErrorServers)
            {
                var server = await Client.GetChannelAsync(s.ChannelId);
                if (null == server)
                {
                    AppleService.DeleteErrorServer(s, db);
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
            _logger.Error(e);
            Environment.Exit(1);
        }
    }

    public async Task<IThreadChannel?> CreateThreadAsync(Thread thread, Update update)
    {
        var channel = await Client.GetChannelAsync(thread.ChannelId) as ITextChannel;
        if (channel == null)
        {
            _logger.Error($"Channel {thread.ChannelId} does not exist");
            return null;
        }

        _logger.Info($"Creating thread in {channel.Name} for {update.VersionReadable}");
        return await channel.CreateThreadAsync($"{update.VersionReadable} Release Thread");
    }

    public async Task<IThreadChannel?> CreateForumAsync(Forum forum, Update update)
    {
        var dForum = await Client.GetChannelAsync(forum.ChannelId) as IForumChannel;
        if (null == dForum)
        {
            _logger.Error($"forum {forum.ChannelId} does not exist");
            return null;
        }

        _logger.Info($"Creating post in {dForum.Name} for {update.VersionReadable}");
        var embed = new EmbedBuilder()
        {
            Color = update.Device.Color,
            Title = update.VersionReadable,
        };
        embed.AddField(name: "Build", value: update.Build)
            .AddField(name: "Size", value: update.Size)
            .AddField(name: "Release Date", value: update.ReleaseDate.ToShortDateString())
            .AddField(name: "Changelog", value: update.Device.Changelog);

        var imagePath = GetImagePath(update.Device.Category);
        if (!string.IsNullOrEmpty(imagePath))
        {
            embed.ThumbnailUrl = imagePath;
        }

        return await dForum.CreatePostAsync(title: $"{update.VersionReadable} Discussion",
            text: $"Discuss the release of {update.VersionReadable} here.",
            embed: embed.Build(),
            archiveDuration: ThreadArchiveDuration.OneWeek);
    }

    public async Task<IUserMessage> PostUpdateAsync(Server server, Update update, List<string>? postedThreads = null,
        List<string>? postedForums = null)
    {
        var channel = await Client.GetChannelAsync(server.ChannelId) as ITextChannel;
        if (null == channel)
        {
            _logger.Warn($"Channel with id {server.ChannelId} doesnt exist. Removing");
            AppleService.DeleteServer(server);

            return null!;
        }

        var mention = server.TagId != "" ? $"<@&{server.TagId}>" : "";

        var embed = new EmbedBuilder
        {
            Color = new Color(update.Device.Color),
            Title = $"New {update.Device.FriendlyName} Release!",
            Timestamp = DateTime.Now,
        };
        embed.AddField(name: "Version", value: update.VersionReadable)
            .AddField(name: "Build", value: update.Build)
            .AddField(name: "Size", value: update.Size);

        if (null != postedThreads && postedThreads.Any())
            embed.AddField(name: "Discussion Thread(s)", value: string.Join('\n', postedThreads));
        if (null != postedForums && postedForums.Any())
            embed.AddField(name: "Discussion Forum(s)", value: string.Join('\n', postedForums));
        var isGM = update.VersionReadable.Contains("Golden Master"); // split because testing
        if (isGM)
            embed.AddField(name: "Why Golden Master?", value: "use /whygm to learn why.");


        var imagePath = GetImagePath(update.Device.Category);
        if (!string.IsNullOrEmpty(imagePath))
        {
            embed.ThumbnailUrl = imagePath;
        }

        if (!string.IsNullOrEmpty(update.Device.Changelog))
        {
            embed.Url = update.Device.Changelog;
        }

        _logger.Info($"Posting {update.VersionReadable} to {channel.Name}");
        try
        {
            return await channel.SendMessageAsync(text: mention, embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            await PostError($"Error posting to {channel.Name}. {ex.Message}");
        }

        return null!;
    }

    private string GetImagePath(string category)
    {
        if (category.Contains("ios", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/iphone.png";
        else if (category.Contains("mac", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/mac.png";
        else if (category.Contains("tv", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/tv.png";
        else if (category.Contains("watch", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/watch.png";
        else if (category.Contains("vision", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/vision.png";

        return string.Empty;
    }
}