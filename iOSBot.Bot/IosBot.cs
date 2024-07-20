using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Bot.Singletons;
using iOSBot.Service;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NLog;

namespace iOSBot.Bot
{
    public class IosBot
    {
        // https://discord.com/api/oauth2/authorize?client_id=1126703029618475118&permissions=3136&redirect_uri=https%3A%2F%2Fgithub.com%2FPopulo%2FiOSBetaBot&scope=bot

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IServiceProvider _serviceProvider;

        private DiscordSocketClient? Client { get; set; }
        private DiscordRestClient? RestClient { get; set; }

        private readonly ApiSingleton _apiFeed = ApiSingleton.Instance;

        private string? Status { get; set; }

        public IosBot()
        {
            _serviceProvider = CreateProvider();
        }

        private IServiceProvider CreateProvider()
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages,
                MessageCacheSize = 15
            };

            var collection = new ServiceCollection();

            collection.AddTransient<IAppleService, AppleService>();
            collection
                .AddSingleton(config)
                .AddSingleton<DiscordSocketClient>();

            return collection.BuildServiceProvider();
        }

        public static Task Main(string[] args) => new IosBot().MainAsync(args);

        private async Task MainAsync(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
            RestClient = Client.Rest;

            if (args.Length == 0) throw new Exception("Include token in args");
            switch (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            {
                case "Release":
                    _logger.Info("Environment: Prod");
                    Status = "for new releases";
                    break;
                case "Develop":
                    _logger.Info("Environment: Dev");
                    Status = "in testing mode";
                    break;
            }

            await Client.LoginAsync(TokenType.Bot, args[0]);
            await Client.SetGameAsync(Status, type: ActivityType.Watching);

            await Client.StartAsync();

            Client.Log += _client_Log;
            Client.Ready += _client_Ready;
            Client.SlashCommandExecuted += _client_SlashCommandExecuted;
            Client.MessageReceived += _client_MessageReceived;

            _apiFeed.Bot = Client;
            _apiFeed.Start();

            await Task.Delay(-1);
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg.Channel is not IDMChannel || arg.Author.Id == Client.GetApplicationInfoAsync().Result.Id)
                return;

            Commands.PostError(Client, new AppleService(), $"DM Received:\n{arg.Content}\n-@{arg.Author}");
            await arg.Channel.SendMessageAsync("Sending this message along. thank you.");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error($"{e}\n{e.ExceptionObject}");
            Commands.PostError(Client, new AppleService(), $"Unhandled Exception:\n{e}\n{e.ExceptionObject}");
        }

        private Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            JObject jsonArgs = new JObject();
            foreach (var o in arg.Data.Options)
            {
                if (null != o.Value) jsonArgs.Add(new JProperty(o.Name, o.Value.ToString()));
                else jsonArgs.Add(new JProperty(o.Name, "null"));
            }

            _logger.Info(
                $"Command received: {arg.CommandName} in {RestClient.GetChannelAsync(arg.ChannelId!.Value).Result} from {arg.User.Username}\n```json\nargs:{jsonArgs}\n```");

            switch (arg.CommandName)
            {
                case "watch":
                    Commands.InitCommand(arg, RestClient);
                    break;
                case "unwatch":
                    Commands.RemoveCommand(arg, RestClient);
                    break;
                case "force":
                    Commands.ForceCommand(arg);
                    break;
                case "error":
                    Commands.ErrorCommand(arg, RestClient);
                    break;
                case "noerror":
                    Commands.RemoveErrorCommand(arg);
                    break;
                case "update":
                    Commands.UpdateOptions(arg, Client);
                    break;
                case "manifest":
                    Commands.Manifest(arg);
                    break;
                case "whycraig":
                    Commands.WhyCraig(arg);
                    break;
                case "goodbot":
                    Commands.GoodBot(arg, RestClient);
                    break;
                case "badbot":
                    Commands.BadBot(arg, RestClient);
                    break;
                case "info":
                    Commands.DeviceInfo(arg);
                    break;
                case "when":
                    Commands.When(arg);
                    break;
                case "servers":
                    Commands.GetServers(arg, RestClient);
                    break;
                case "status":
                    Commands.BotStatus(arg, StatusCommand.STATUS);
                    break;
                case "start":
                    Commands.BotStatus(arg, StatusCommand.START);
                    break;
                case "stop":
                    Commands.BotStatus(arg, StatusCommand.STOP);
                    break;
                case "yesthreads":
                    Commands.NewThreadChannel(arg);
                    break;
                case "nothreads":
                    Commands.DeleteThreadChannel(arg);
                    break;
                case "yesforum":
                    Commands.AddForumPost(arg, RestClient);
                    break;
                case "noforum":
                    Commands.RemoveForumPost(arg, RestClient);
                    break;
                case "fake":
                    Commands.FakeUpdate(arg);
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task _client_Ready()
        {
            Commands.UpdateCommands(Client);
        }

        private async Task _client_Log(LogMessage arg)
        {
            if (arg.Message.Contains("Unknown Channel")) return;

            _logger.Info(arg.Message);
            if (null != arg.Exception)
            {
                Commands.PostError(Client, new AppleService(), $"Bot error:\n{arg.Exception.Message}");
                _logger.Error(arg.Exception);
                _logger.Error(arg.Exception.InnerException.StackTrace);
            }
        }
    }
}