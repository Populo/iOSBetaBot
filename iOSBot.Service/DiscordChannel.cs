namespace iOSBot.Service;

public class DiscordChannel
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public DiscordServer Server { get; set; }
}