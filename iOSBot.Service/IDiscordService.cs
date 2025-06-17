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
    public Task<IThreadChannel> CreateThread(Thread thread, Update2 update);
    public Task<IThreadChannel> CreateForum(Forum forum, Update2 update);

    public Task<IUserMessage> PostUpdate(Server server, Update2 update, List<string>? postedThreads = null,
        List<string>? postedForums = null);

    Task SetStatus(UserStatus status);
    Task SetActivity(string activity);
    int GetServerCount();
    Task PostError(string message);
    List<ErrorServer> GetErrorServers();
}

public class DiscordService(
    ILogger<DiscordService> logger,
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
                    Category = s.Track.ToString()
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

    public async Task<IThreadChannel> CreateThread(Thread thread, Update2 update)
    {
        var channel = await RestClient.GetChannelAsync(thread.ChannelId) as ITextChannel;
        if (channel == null)
        {
            logger.LogError("Channel {ThreadChannelId} does not exist", thread.ChannelId);
            return null;
        }

        logger.LogInformation($"Creating thread in {channel.Name} for {update.Version}");
        return await channel.CreateThreadAsync($"{update.Version} Release Thread");
    }

    public async Task<IThreadChannel> CreateForum(Forum forum, Update2 update)
    {
        var dForum = await RestClient.GetChannelAsync(forum.ChannelId) as IForumChannel;
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

    public async Task<IUserMessage> PostUpdate(Server server, Update2 update, List<string>? postedThreads = null,
        List<string>? postedForums = null)
    {
        ITextChannel channel;
        try
        {
            var guild = socketClient.GetGuild(server.ServerId)
                        ?? throw new Exception("Guild not found");
            channel = guild.GetTextChannel(server.ChannelId)
                      ?? throw new Exception("Channel not found");
            if (null == channel)
            {
                logger.LogWarning("Channel with id {ServerChannelId} doesnt exist.", server.ChannelId);
                return null!;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            if (e.Message.Contains("Guild"))
            {
                await PostError(
                    $"Cannot get guild {server.ServerId}. Member: {socketClient.Guilds.Any(g => g.Id == server.ServerId)} Track: {update.TrackName}");
            }
            else
            {
                await PostError($"Cannot get channel {server.ChannelId}. Track: {update.TrackName}");
            }

            return null;
        }


        var mention = server.TagId != "" ? $"<@&{server.TagId}>" : "";

        var embed = new EmbedBuilder
        {
            Color = new Color(update.Color),
            Title = $"New {update.TrackName} Release!",
            Timestamp = update.ReleaseDate,
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

        logger.LogInformation($"Posting {update.Version} to {channel.Name}");
        try
        {
            return await channel.SendMessageAsync(text: mention, embed: embed.Build());
        }
        catch (Exception ex)
        {
            logger.LogError(420, ex, "Error posting to {ChannelName}. {ErrorMessage}", channel.Name, ex.Data);
            await PostError($"Error posting to {channel.Name}. {ex.Message}");
            StartRecovery(server, update);
        }

        return null!;
    }

    private string GetImagePath(string category)
    {
        if (category.Contains("ios", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/CraigBotAssets/8cd0f1d8e1f04b96033f4018dc844b94efd5c34a/EmbedImages/ios.png";
        if (category.Contains("mac", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/CraigBotAssets/8cd0f1d8e1f04b96033f4018dc844b94efd5c34a/EmbedImages/macos.png";
        if (category.Contains("tv", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/tv.png";
        if (category.Contains("watch", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/CraigBotAssets/8cd0f1d8e1f04b96033f4018dc844b94efd5c34a/EmbedImages/watchos.png";
        if (category.Contains("vision", StringComparison.CurrentCultureIgnoreCase))
            return
                "https://raw.githubusercontent.com/Populo/iOSBetaBot/dfde2d531977c471caad016788960127f2f09f6a/iOSBot.Bot/Images/vision.png";

        return string.Empty;
    }

    private void StartRecovery(Server server, Update2 update)
    {
        // get server owner
        var owner = socketClient.Guilds.First(g => g.Id == server.ServerId).Owner
                    ?? throw new Exception($"Cannot get server owner for {server.ServerId}");

        // get channel that failed
        var channel = socketClient.GetChannel(server.ChannelId) as ITextChannel
                      ?? throw new Exception($"Cannot get channel {server.ChannelId}");

        // craft DM
        var message =
            $"Hello! I am trying to post an update for {update.TrackName} in the channel {channel.Name} but I am having trouble. " +
            $"Please double check the permissions for me in that channel and use /test to verify I can post there. " +
            $"Once that is done, re-register the alerts for this track using /watch {update.TrackName} to make sure you will continue to be notified.\n\n" +
            $"Respond to this DM with questions and my creator will respond. Thanks!";

        // send DM
        owner.SendMessageAsync(message);
    }
}