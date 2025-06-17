namespace iOSBot.Console.Migrations;

public partial class ErrorServer
{
    public Guid Id { get; set; }

    public ulong ServerId { get; set; }

    public ulong ChannelId { get; set; }
}