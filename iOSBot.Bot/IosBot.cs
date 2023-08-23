using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Data;
using Newtonsoft.Json;
using NLog;
using System.Net.WebSockets;

namespace iOSBot.Bot
{
    public class IosBot
    {
        // https://discord.com/api/oauth2/authorize?client_id=1133469416458301510&permissions=133120&scope=bot

        private Logger Logger = LogManager.GetCurrentClassLogger();

        private DiscordSocketClient _client { get; set; }
        private DiscordRestClient _restClient { get; set; }

        private ApiSingleton apiFeed = ApiSingleton.Instance;

        private string Status { get; set; }

        public static Task Main(string[] args) => new IosBot().MainAsync(args);

        private async Task MainAsync(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            };

            if (args.Length == 0) { throw new Exception("Provide Token"); }

            _client = new DiscordSocketClient(config);
            _restClient = _client.Rest;

            await _client.LoginAsync(TokenType.Bot, args[0]);

#if DEBUG
            Status = "in testing mode";
            Logger.Info("Environment: Dev");
#else
            Status = "for new releases";
            Logger.Info("Environment: Prod");
#endif
            await _client.SetGameAsync(Status, type: ActivityType.Watching);

            await _client.StartAsync();

            _client.Log += _client_Log;
            _client.Ready += _client_Ready;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;
            _client.LoggedOut += _client_LoggedOut;
            //_client.MessageReceived += _client_MessageReceived;

            apiFeed.Bot = _restClient;
            apiFeed.Start();

            await Task.Delay(-1);
        }

        private Task _client_MessageReceived(SocketMessage arg)
        {
            return Task.CompletedTask;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error(e);
        }

        private Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            Logger.Info($"Command received: {arg.CommandName} in {_restClient.GetChannelAsync(arg.ChannelId.Value).Result}");

            switch (arg.CommandName)
            {
                case "watch":
                    Commands.InitCommand(arg, _restClient);
                    break;
                case "unwatch":
                    Commands.RemoveCommand(arg, _restClient);
                    break;
                case "force":
                    Commands.ForceCommand(arg, _restClient);
                    break;
                case "error":
                    Commands.ErrorCommand(arg, _restClient);
                    break;
                case "noerror":
                    Commands.RemoveErrorCommand(arg, _restClient);
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

                await _client.CreateGlobalApplicationCommandAsync(forceCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(errorCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(removeErrorCommand.Build());
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

            await _client.CreateGlobalApplicationCommandAsync(initCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(removeCommand.Build());
        }

        private async Task _client_Log(LogMessage arg)
        {
            Logger.Info(arg.Message);
            if (null != arg.Exception)
            {
                if (arg.Exception.GetType() != typeof(WebSocketException)) {
                    apiFeed.PostError(arg.Exception.Message);
                }
                Logger.Error(arg.Exception);
            }

            return;
        }

        private Task _client_LoggedOut()
        {
            apiFeed.PostError("Logging Out");
            return Task.CompletedTask;
        }

        List<Device> GetDevices()
        {
            using var db = new BetaContext();

            return db.Devices.ToList();
        }
    }
}