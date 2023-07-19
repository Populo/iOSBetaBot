using Discord;
using Discord.Net;
using Discord.WebSocket;
using iOSBot.Data;
using Newtonsoft.Json;
using NLog;

namespace iOSBot.Bot
{
    public class IosBot
    {
        private Logger Logger = LogManager.GetCurrentClassLogger();

        private DiscordSocketClient _client { get; set; }

        private ApiSingleton apiFeed = ApiSingleton.Instance;

        public static Task Main(string[] args) => new IosBot().MainAsync(args);

        private async Task MainAsync(string[] args)
        {
            if (args.Length == 0) { throw new Exception("Provide Token"); }

            _client = new DiscordSocketClient();

            await _client.LoginAsync(TokenType.Bot, args[0]);
            await _client.SetGameAsync("for new releases", type: ActivityType.Watching);
            await _client.StartAsync();

            _client.Log += _client_Log;
            _client.Ready += _client_Ready;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;

            apiFeed.Bot = _client;
            apiFeed.Start();

            await Task.Delay(-1);
        }

        private Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            Logger.Info($"Command received: {arg.CommandName} in {arg.Channel}");

            switch (arg.CommandName)
            {
                case "watch":
                    Commands.InitCommand(arg);
                    break;
                case "unwatch":
                    Commands.RemoveCommand(arg);
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

            foreach (var c in Helpers.CategoryColors)
            {
                param.AddChoice(c.CategoryFriendly, c.Category);
            }

            initCommand.AddOption(param);
            removeCommand.AddOption(param);

            initCommand.DefaultMemberPermissions = GuildPermission.ManageGuild;
            removeCommand.DefaultMemberPermissions = GuildPermission.ManageGuild;

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(initCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(removeCommand.Build());
            }
            catch (HttpException e)
            {
                var json = JsonConvert.SerializeObject(e.Reason, Formatting.Indented);
                Logger.Error(json);
            }
            
        }

        private Task _client_Log(LogMessage arg)
        {
            Logger.Info(arg.Exception);

            return Task.CompletedTask;
        }
    }
}