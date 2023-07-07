using Discord.Rest;

namespace iOSBetaBot
{
    internal class IosBot
    {
        private DiscordRestClient _client { get; set; }

        static void Main(string[] args) => new IosBot().MainAsync(args);

        private void MainAsync(string[] args)
        {
            if (args.Length == 0) { throw new Exception("Provide Token"); }

            _client = new DiscordRestClient();

            _client.LoginAsync(Discord.TokenType.Bot, args[0]);
        }
    }
}