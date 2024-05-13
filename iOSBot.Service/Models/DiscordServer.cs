namespace iOSBot.Service.Models;

public class DiscordServer
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public List<DiscordChannel> Channels { get; set; }
}