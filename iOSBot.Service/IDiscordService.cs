using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using Microsoft.Extensions.Logging;
using Thread = iOSBot.Data.Thread;

namespace iOSBot.Service;

public interface IDiscordService
{
    public DiscordServer GetServerAndChannels(ulong serverId);
    public Task<RestChannel> GetChannel(ulong channelId);
    public Task<IUserMessage> SendMessage(ulong channelId, string message, Embed? embed = null);
    public Task<IThreadChannel> CreateThread(Thread thread, Update update);
    public Task<IThreadChannel> CreateForum(Forum forum, Update update);

    public Task<IUserMessage> PostUpdate(Server server, Update update, List<string>? postedThreads = null,
        List<string>? postedForums = null);

    Task SetStatus(UserStatus status);
    Task SetActivity(string activity);
    int GetServerCount();
    Task PostError(string message);
    List<ErrorServer> GetErrorServers();
}

public class DiscordService(
    ILogger<DiscordService> logger,
    IAppleService appleService,
    DiscordSocketClient socketClient)
    : IDiscordService
{
    private DiscordRestClient RestClient => socketClient.Rest;

    public async Task SetStatus(UserStatus status) => await socketClient.SetStatusAsync(status);

    public async Task SetActivity(string activity) => await socketClient.SetCustomStatusAsync(activity);

    public int GetServerCount() => socketClient.Guilds.Count;

    public List<ErrorServer> GetErrorServers()
    {
        using var db = new BetaContext();
        return db.ErrorServers.ToList();
    }

    public async Task PostError(string message)
    {
        var errorServers = GetErrorServers();
        await Parallel.ForEachAsync(errorServers, async (server, _) =>
        {
            var channel = await RestClient.GetChannelAsync(server.ChannelId) as ITextChannel;
            if (null == channel)
            {
                logger.LogCritical(42069, "Cannot get error posting server.");
                return;
            }

            await channel.SendMessageAsync(message);
        });
    }

    public DiscordServer GetServerAndChannels(ulong serverId)
    {
        using var db = new BetaContext();
        var server = RestClient.GetGuildAsync(serverId).Result;

        return new DiscordServer()
        {
            Name = server.Name,
            Id = server.Id,
            Channels = db.Servers.Where(s => s.ServerId == serverId)
                .AsEnumerable()
                .Select(s => new DiscordChannel()
                {
                    Id = s.ChannelId,
                    Name = server.GetChannelAsync(s.ChannelId).Result.Name,
                    Category = s.Category
                }).ToList()
        };
    }

    public async Task<RestChannel> GetChannel(ulong channelId)
    {
        return await RestClient.GetChannelAsync(channelId);
    }

    public async Task<IUserMessage> SendMessage(ulong channelId, string message, Embed? embed = null)
    {
        if (await RestClient.GetChannelAsync(channelId) is ITextChannel channel)
        {
            return await channel.SendMessageAsync(message);
        }

        logger.LogError("Channel {ChannelId} does not exist", channelId);
        return null;
    }

    public async Task<IThreadChannel> CreateThread(Thread thread, Update update)
    {
        var channel = await RestClient.GetChannelAsync(thread.ChannelId) as ITextChannel;
        if (channel == null)
        {
            logger.LogError("Channel {ThreadChannelId} does not exist", thread.ChannelId);
            return null;
        }

        logger.LogInformation($"Creating thread in {channel.Name} for {update.VersionReadable}");
        return await channel.CreateThreadAsync($"{update.VersionReadable} Release Thread");
    }

    public async Task<IThreadChannel> CreateForum(Forum forum, Update update)
    {
        var dForum = await RestClient.GetChannelAsync(forum.ChannelId) as IForumChannel;
        if (null == dForum)
        {
            logger.LogError("forum {ForumChannelId} does not exist", forum.ChannelId);
            return null;
        }

        logger.LogInformation($"Creating post in {dForum.Name} for {update.VersionReadable}");
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

    public async Task<IUserMessage> PostUpdate(Server server, Update update, List<string>? postedThreads = null,
        List<string>? postedForums = null)
    {
        var channel = await RestClient.GetChannelAsync(server.ChannelId) as ITextChannel;
        if (null == channel)
        {
            logger.LogWarning("Channel with id {ServerChannelId} doesnt exist. Removing", server.ChannelId);
            appleService.DeleteServer(server);

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

        logger.LogInformation($"Posting {update.VersionReadable} to {channel.Name}");
        try
        {
            return await channel.SendMessageAsync(text: mention, embed: embed.Build());
        }
        catch (Exception ex)
        {
            logger.LogError(420, ex, "Error posting to {ChannelName} ({channelId}). {ErrorMessage}", channel.Name, server.channelId, ex.Message);
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