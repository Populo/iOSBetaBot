using System.Collections.Concurrent;
using System.Timers;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using iOSBot.Bot.Commands;
using iOSBot.Data;
using iOSBot.Service;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NLog;
using Timer = System.Timers.Timer;

namespace iOSBot.Bot
{
    public class IosBot
    {
        // https://discord.com/api/oauth2/authorize?client_id=1126703029618475118&permissions=3136&redirect_uri=https%3A%2F%2Fgithub.com%2FPopulo%2FiOSBetaBot&scope=bot

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly IServiceProvider _serviceProvider;

        private DiscordSocketClient Client { get; set; }
        private DiscordRestClient RestClient { get; set; }

        private IDiscordService Discord { get; set; }
        private IAppleService Apple { get; set; }
        
        public Timer Timer { get; set; }

        private string? Status { get; set; }

        public IosBot()
        {
            _serviceProvider = CreateProvider() ?? throw new Exception("cannot create service provider");

            Discord = _serviceProvider.GetService<IDiscordService>() ?? throw new Exception("Cannot create Discord service");
            Apple = _serviceProvider.GetService<IAppleService>() ?? throw new Exception("Cannot create Apple service");;
            
            Timer = new Timer
            {
                Interval = 90 * 1000 // 90 seconds
            };
            
            Timer.Elapsed += TimerOnElapsed;
            Timer.Start();
        }

        private IServiceProvider CreateProvider()
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.GuildMembers,
                MessageCacheSize = 15
            };

            var collection = new ServiceCollection();

            collection.AddScoped<IAppleService, AppleService>();
            collection.AddScoped<IDiscordService, DiscordService>();
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
            Client.JoinedGuild += ClientOnJoinedGuild;

            await Task.Delay(-1);
        }

        private Task ClientOnJoinedGuild(SocketGuild arg)
        {
            using var db = new BetaContext();

            Discord.PostToServers(Client, db.ErrorServers.Select(s => s.ServerId), $"Joined Server: {arg.Name}");
            return Task.CompletedTask;
        }

        private Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg.Channel is not IDMChannel || arg.Author.Id == Client.GetApplicationInfoAsync().Result.Id) return Task.CompletedTask;
            using var db = new BetaContext();
            
            Discord.PostToServers(Client, db.ErrorServers.Select(s => s.ServerId), $"DM Received:\n{arg.Content}\n-@{arg.Author}");
            arg.Channel.SendMessageAsync("Sending this message along. thank you.");

            return Task.CompletedTask;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            using var db = new BetaContext();

            _logger.Error($"{e}\n{e.ExceptionObject}");
            Discord.PostToServers(Client, db.ErrorServers.Select(s => s.ServerId), $"Unhandled Exception:\n{e}\n{e.ExceptionObject}");
        }

        private Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            JObject jsonArgs = new JObject();
            foreach (var o in arg.Data.Options)
            {
                jsonArgs.Add(new JProperty(o.Name, o.Value.ToString()));
            }
            _logger.Info($"Command received: {arg.CommandName} in {RestClient.GetChannelAsync(arg.ChannelId!.Value).Result} from {arg.User.Username}\n```json\nargs:{jsonArgs}\n```");

            switch (arg.CommandName)
            {
                case "watch":
                    AppleCommands.YesWatch(arg, RestClient);
                    break;
                case "unwatch":
                    AppleCommands.NoWatch(arg, RestClient);
                    break;
                case "force":
                    if (!AdminCommands.IsAllowed(arg.User.Id))
                    {
                        arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                    }

                    Timer.Stop();
                    TimerOnElapsed(null, null!);
                    Timer.Start();
                    arg.RespondAsync("Updates Checked", ephemeral: true);
                    break;
                case "error":
                    AdminCommands.YesErrors(arg, RestClient);
                    break;
                case "noerror":
                    AdminCommands.NoErrors(arg);
                    break;
                case "update":
                    AdminCommands.UpdateOptions(arg, Client);
                    break;
                case "manifest":
                    MemeCommands.Manifest(arg);
                    break;
                case "goodbot":
                    MemeCommands.GoodBot(arg, RestClient);
                    break;
                case "badbot":
                    MemeCommands.BadBot(arg, RestClient);
                    break;
                case "info":
                    AppleCommands.DeviceInfo(arg);
                    break;
                case "servers":
                    AdminCommands.GetServers(arg, Client);
                    break;
                case "status":
                    string response = Timer.Enabled ? "Running" : "Not Running";
                    arg.RespondAsync(ephemeral: true, text: $"Bot is currently: {response}");
                    break;
                case "start":
                    if (!AdminCommands.IsAllowed(arg.User.Id))
                    {
                        arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                    }

                    Timer.Start();
                    arg.FollowupAsync(ephemeral: true, text: $"Bot is running");
                    break;
                case "stop":
                    if (!AdminCommands.IsAllowed(arg.User.Id))
                    {
                        arg.RespondAsync("Only the bot creator can use this command.", ephemeral: true);
                    }

                    Timer.Stop();
                    arg.FollowupAsync(ephemeral: true, text: $"Bot is stopped");
                    break;
                case "yesthreads":
                    AppleCommands.NewThreadChannel(arg);
                    break;
                case "nothreads":
                    AppleCommands.DeleteThreadChannel(arg);
                    break;
                case "fake":
                    AdminCommands.FakeUpdate(arg, Discord);
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task _client_Ready()
        {
            CommandInitializer.UpdateCommands(Client);
        }

        private async Task _client_Log(LogMessage arg)
        {
            _logger.Info(arg.Message);
            if (null != arg.Exception)
            {
                // dont care about logging these
                if (arg.Exception.Message.EndsWith("Server requested a reconnect") ||
                    arg.Exception.Message.EndsWith("WebSocket connection was closed")) return;
                
                using var db = new BetaContext();
                Discord.PostToServers(Client, db.ErrorServers.Select(s => s.ChannelId), $"Bot error:\n{arg.Exception.Message}");
                _logger.Error(arg.Exception);
                _logger.Error(arg.Exception.InnerException.StackTrace);
            }
        }
        
        private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            var updates = new ConcurrentBag<Service.Update>();
            using var db = new BetaContext();
            var dbUpdates = db.Updates.ToList();

            // check all devices for /info 
            Parallel.ForEach(db.Devices, device =>
            {
                try
                {
                    Service.Update u = Apple.GetUpdate(device).Result;
                    
                    if (!dbUpdates.Any(up => up.Build == u.Build &&
                                             up.ReleaseDate == u.ReleaseDate &&
                                             up.Category == u.Group)) updates.Add(u);
                    else _logger.Info("No new update found for " + device.FriendlyName);
                }
                catch (Exception ex)
                {
                    Discord.PostToServers(Client, db.ErrorServers.Select(s => s.ChannelId), $"Error checking update for {device.FriendlyName}:\n{ex.Message}");
                }
            });
            
            foreach (var update in updates)
            {
                var category = update.Device.Category;
                var servers = db.Servers.Where(s => s.Category == category);
                var threads = db.Threads.Where(t => t.Category == category);

                _logger.Info($"Update for {update.Device.FriendlyName} found. Version {update.VersionReadable} with build id {update.Build}");

                // dont post if older than 12 hours, still add to db tho
                var postOld = Convert.ToBoolean(Convert.ToInt16(db.Configs.First(c => c.Name == "PostOld").Value));
                
                if (postOld || update.ReleaseDate.DayOfYear == DateTime.Today.DayOfYear)
                {    
                    foreach (var server in servers)
                    {
                        Discord.PostUpdate(update, server);
                    }

                    foreach (var thread in threads)
                    {
                        Discord.PostThread(update, thread);
                    }
                }
                else
                {
                    var error = $"{update.Device.FriendlyName} update {update.VersionReadable}-{update.Build} was released on {update.ReleaseDate.ToShortDateString()}. too old. not posting.";
                    _logger.Info(error);
                    Discord.PostToServers(Client, db.ErrorServers.Select(s => s.ChannelId), error);
                }
                
                Apple.SaveUpdate(update);
            }
            
            // update server count
            ulong channelId = ulong.Parse(db.Configs.First(c => c.Name == "StatusChannel").Value);
            string env = db.Configs.First(c => c.Name == "Environment").Value;
            IChannel channel = Client.GetChannelAsync(channelId).Result;
            await ((IVoiceChannel)channel).ModifyAsync(c => c.Name = $"{env} Bot Servers: {RestClient.GetGuildsAsync().Result.Count}");

            Timer.Interval = int.Parse(db.Configs.First(c => c.Name == "Timer").Value);

            await db.SaveChangesAsync();
        }
    }
}