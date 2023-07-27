using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using NLog;

namespace iOSBot.Bot
{
    public class IosBot
    {
        // https://discord.com/api/oauth2/authorize?client_id=1133469416458301510&permissions=133120&scope=bot

        private Logger Logger = LogManager.GetCurrentClassLogger();

        private DiscordSocketClient _client { get; set; }
        private DiscordRestClient _restClient { get; set; }

        private ApiSingleton apiFeed = ApiSingleton.Instance;

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
            await _client.SetGameAsync("in testing mode");
#else
            await _client.SetGameAsync("for new releases", type: ActivityType.Watching);
#endif
            await _client.StartAsync();

            _client.Log += _client_Log;
            _client.Ready += _client_Ready;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;
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
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task _client_Ready()
        {
            var initCommand = new SlashCommandBuilder();
            var removeCommand = new SlashCommandBuilder();
            var forceCommand = new SlashCommandBuilder();

            initCommand.WithName("watch");
            removeCommand.WithName("unwatch");
            forceCommand.WithName("force");

            initCommand.WithDescription("Begin posting OS updates to this channel");
            removeCommand.WithDescription("Discontinue posting updates to this channel");
            forceCommand.WithDescription("Force bot to check for new updates");

            var param = new SlashCommandOptionBuilder()
            {
                Name = "category",
                Description = "Which OS updates",
                IsRequired = true,
                Type = ApplicationCommandOptionType.String
            };

            foreach (var c in Helpers.CategoryColors)
            {
                param.AddChoice(c.CategoryFriendly, c.Category);
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
            forceCommand.DefaultMemberPermissions = GuildPermission.ManageGuild;

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(initCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(removeCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(forceCommand.Build());
            }
            catch (HttpException e)
            {
                var json = JsonConvert.SerializeObject(e.Reason, Formatting.Indented);
                Logger.Error(json);
            }
            
        }

        private Task _client_Log(LogMessage arg)
        {
            Logger.Info(arg.Message);
            if (null != arg.Exception)
            {
                Logger.Error(arg.Exception);
            }

            return Task.CompletedTask;
        }
    }
}