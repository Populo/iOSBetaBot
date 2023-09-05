using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog;
using iOSBot.Service;
using Microsoft.EntityFrameworkCore;
using Update = iOSBot.Service.Update;

namespace iOSBot.Bot
{
    public class IosBot
    {
        // https://discord.com/api/oauth2/authorize?client_id=1133469416458301510&permissions=133120&scope=bot

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
                GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent
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
            

            if (args.Length == 0) { throw new Exception("Provide Token"); }

            Client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
            RestClient = Client.Rest;

            await Client.LoginAsync(TokenType.Bot, args[0]);

#if DEBUG
            Status = "in testing mode";
            _logger.Info("Environment: Dev");
#else
            Status = "for new releases";
            _logger.Info("Environment: Prod");
#endif
            await Client.SetGameAsync(Status, type: ActivityType.Watching);

            await Client.StartAsync();

            Client.Log += _client_Log;
            Client.Ready += _client_Ready;
            Client.SlashCommandExecuted += _client_SlashCommandExecuted;
            Client.LoggedOut += _client_LoggedOut;
            //_client.MessageReceived += _client_MessageReceived;

            _apiFeed.Bot = RestClient;
            _apiFeed.Start();

            await Task.Delay(-1);
        }

        private Task _client_MessageReceived(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error(e);
            _apiFeed.PostError(e.ToString());
        }

        private Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            _logger.Info($"Command received: {arg.CommandName} in {RestClient.GetChannelAsync(arg.ChannelId!.Value).Result}");

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
            _logger.Info(arg.Message);
            if (null != arg.Exception)
            {
                _apiFeed.PostError(arg.Exception.Message);
                _logger.Error(arg.Exception);
            }
        }

        private Task _client_LoggedOut()
        {
            _apiFeed.PostError("Logging Out");
            return Task.CompletedTask;
        }
    }
}