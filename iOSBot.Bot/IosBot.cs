using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NLog;
using iOSBot.Service;

namespace iOSBot.Bot
{
    public class IosBot
    {
        // https://discord.com/api/oauth2/authorize?client_id=1133469416458301510&permissions=133120&scope=bot

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IServiceProvider _serviceProvider;

        private DiscordSocketClient Client { get; set; }
        private DiscordRestClient RestClient { get; set; }

        private ApiSingleton _apiFeed = ApiSingleton.Instance;

        private string Status { get; set; }

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
            Logger.Info("Environment: Prod");
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
            _apiFeed.PostError(e.ExceptionObject.ToString());
        }

        private Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            _logger.Info($"Command received: {arg.CommandName} in {RestClient.GetChannelAsync(arg.ChannelId.Value).Result}");

            switch (arg.CommandName)
            {
                case "watch":
                    Commands.InitCommand(arg, RestClient);
                    break;
                case "unwatch":
                    Commands.RemoveCommand(arg, RestClient);
                    break;
                case "force":
                    Commands.ForceCommand(arg, RestClient);
                    break;
                case "error":
                    Commands.ErrorCommand(arg, RestClient);
                    break;
                case "noerror":
                    Commands.RemoveErrorCommand(arg, RestClient);
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task _client_Ready()
        {
            try
            {
                WatchUnwatch();

                var forceCommand = new SlashCommandBuilder();
                var errorCommand = new SlashCommandBuilder();
                var removeErrorCommand = new SlashCommandBuilder();
       
                forceCommand.WithName("force");
                errorCommand.WithName("error");
                removeErrorCommand.WithName("noerror");


                forceCommand.WithDescription("Force bot to check for new updates");
                errorCommand.WithDescription("Post bot errors to this channel");
                removeErrorCommand.WithDescription("Dont post bot errors to this channel");


                forceCommand.DefaultMemberPermissions = GuildPermission.ManageGuild;
                errorCommand.DefaultMemberPermissions = GuildPermission.Administrator;
                removeErrorCommand.DefaultMemberPermissions = GuildPermission.Administrator;

                await Client.CreateGlobalApplicationCommandAsync(forceCommand.Build());
                await Client.CreateGlobalApplicationCommandAsync(errorCommand.Build());
                await Client.CreateGlobalApplicationCommandAsync(removeErrorCommand.Build());
            }
            catch (HttpException e)
            {
                var json = JsonConvert.SerializeObject(e.Reason, Formatting.Indented);
                await _client_Log(new LogMessage(LogSeverity.Error, "_client_Ready", json, e));
            }
        }

        public async void WatchUnwatch()
        {
            var initCommand = new SlashCommandBuilder();
            var removeCommand = new SlashCommandBuilder();

            initCommand.WithName("watch");
            removeCommand.WithName("unwatch");

            initCommand.WithDescription("Begin posting OS updates to this channel");
            removeCommand.WithDescription("Discontinue posting updates to this channel");

            var param = new SlashCommandOptionBuilder()
            {
                Name = "category",
                Description = "Which OS updates",
                IsRequired = true,
                Type = ApplicationCommandOptionType.String
            };

            var devices = GetDevices();

            foreach (var c in devices)
            {
                param.AddChoice(c.FriendlyName, c.Category);
            }

            initCommand.AddOption(param);
            removeCommand.AddOption(param);

            var roleParam = new SlashCommandOptionBuilder()
            {
                Name = "role",
                Description = "Role to ping",
                IsRequired = false,
                Type = ApplicationCommandOptionType.Role
            };

            initCommand.AddOption(roleParam);

            initCommand.DefaultMemberPermissions = GuildPermission.ManageGuild;
            removeCommand.DefaultMemberPermissions = GuildPermission.ManageGuild;

            await Client.CreateGlobalApplicationCommandAsync(initCommand.Build());
            await Client.CreateGlobalApplicationCommandAsync(removeCommand.Build());
        }

        private async Task _client_Log(LogMessage arg)
        {
            _logger.Info(arg.Message);
            if (null != arg.Exception)
            {
                _apiFeed.PostError(arg.Exception.Message);
                _logger.Error(arg.Exception);
            }

            return;
        }

        private Task _client_LoggedOut()
        {
            _apiFeed.PostError("Logging Out");
            return Task.CompletedTask;
        }

        List<Device> GetDevices()
        {
            using var db = new BetaContext();

            return db.Devices.ToList();
        }
    }
}