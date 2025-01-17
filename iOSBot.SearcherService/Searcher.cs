using iOSBot.Service;

namespace iOSBot.SearcherService;

public class Searcher
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();
        builder.Services.AddTransient<IBotService, BotService>();
        builder.Services.AddTransient<IAppleService, AppleService>();
        builder.Services.AddTransient<IMqService, MqService>();

        var host = builder.Build();
        host.Run();
    }
}