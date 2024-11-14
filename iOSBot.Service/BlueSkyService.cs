using Bluesky.Net;
using Bluesky.Net.Commands.AtProto.Server;
using Bluesky.Net.Commands.Bsky.Feed;
using Bluesky.Net.Models;
using Bluesky.Net.Multiples;
using NLog;

namespace iOSBot.Service;

public class BlueSkyService
{
    private readonly IBlueskyApi _bluesky;
    private Logger _logger = LogManager.GetCurrentClassLogger();

    public BlueSkyService(IBlueskyApi bluesky)
    {
        _bluesky = bluesky;

        _ = Auth();
    }

    public async Task Auth()
    {
        Login loginCommand = new("craigbot.bsky.social", Environment.GetEnvironmentVariable("CraigbotBskyPass")!);
        Multiple<Session, Error> result = await _bluesky.Login(loginCommand, CancellationToken.None);

        result.Switch(
            session => _logger.Info($"Successfully logged in"),
            error => throw new Exception(error.ToString())
        );
    }

    public async Task PostUpdate(string message)
    {
        CreatePost post = new(message);
        Multiple<CreatePostResponse, Error> postResponse = await _bluesky.CreatePost(post, CancellationToken.None);

        postResponse.Switch(
            response => _logger.Info($"Successfully posted post {post}"),
            error => throw new Exception(error.ToString())
        );
    }
}