namespace iOSBot.Console.Migrations;

public partial class Forum
{
    public Guid Id { get; set; }

    public ulong ServerId { get; set; }

    public ulong ChannelId { get; set; }

    public string Category { get; set; } = null!;
}