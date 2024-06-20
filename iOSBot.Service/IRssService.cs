using System.Text.RegularExpressions;
using System.Xml.Serialization;
using iOSBot.Data;
using iOSBot.Service.Responses;
using NLog;
using RestSharp;

namespace iOSBot.Service;

public interface IRssService
{
    Task<IEnumerable<Update>> GetRSSUpdates();
}

public class RssService : IRssService
{
    Logger _logger = LogManager.GetCurrentClassLogger();
    private RestClient RestClient { get; set; }

    private Dictionary<string, string> CategoryMap;

    public RssService()
    {
        using var db = new BetaContext();

        var restOptions = new RestClientOptions()
        {
            BaseUrl = new Uri(db.Configs.First(c => c.Name.Equals("RssFeed")).Value)
        };

        RestClient = new RestClient(restOptions);

        CategoryMap = new Dictionary<string, string>()
        {
            { "iOS-beta", "iOSDev" },
            { "iOS-stable", "iOSStable" },
            { "macOS-beta", "macOSDev" },
            { "macOS-stable", "macOSStable" },
            { "tvOS-beta", "tvOSDev" },
            { "tvOS-stable", "tvOSStable" },
            { "watchOS-beta", "watchOSDev" },
            { "watchOS-stable", "watchOSStable" },
            { "visionOS-beta", "visionOSDev" },
            { "visionOS-stable", "visionOSStable" },
            { "airpods-beta", "airpodsDev" },
            { "airpods-stable", "airpodsStable" },
            { "xcode-beta", "xcodeDev" },
            { "xcode-stable", "xcodeStable" }
        };
    }

    public async Task<IEnumerable<Update>> GetRSSUpdates()
    {
        _logger.Info("Getting RSS Feed");
        var request = new RestRequest();
        using var db = new BetaContext();
        List<Update> releases = new();

        var response = await RestClient.GetAsync(request);

        if (!response.IsSuccessful) throw new Exception("Cannot get RSS Feed");
        var xml = response.Content ?? throw new Exception("Cannot read RSS XML");
        RssResponse rss;
        var versionRegex = new Regex(@"\b\d+(\.\d+)*\b");

        using (var reader = new StringReader(xml))
        {
            rss = new XmlSerializer(typeof(RssResponse)).Deserialize(reader) as RssResponse
                  ?? throw new Exception("Cannot deserialize XML");
        }

        foreach (var release in rss.channel.item)
        {
            /* I hate all of this
             * examples:
             * macOS 14.6 beta (23G5052d)
             * AirPods Firmware beta (7A5220e)
             * iPadOS 18 beta (22A5282m)
             * App Store Connect API 3.5
             * TestFlight update
             */

            // skip releases without a build (dont care)
            if (!release.title.Contains('(')) continue;
            var build = release.title.Split('(').Last()
                .Split(')').First();
            // skip if we have this update already
            if (db.Updates.Any(u => u.Build.Equals(build, StringComparison.CurrentCultureIgnoreCase))) continue;

            var platform = release.title.Split(' ').First();
            var version = versionRegex.Match(release.title).Value;
            var type = release.title.Contains("beta", StringComparison.CurrentCultureIgnoreCase)
                ? "beta"
                : "stable";

            releases.Add(new Update()
            {
                Source = "RSS",
                Build = build,
                Version = version,
                ReleaseDate = DateTime.Parse(release.pubDate),
                ReleaseType = type,
                Group = CategoryMap[$"{platform}-{type}"],
            });
        }

        return releases;
    }
}