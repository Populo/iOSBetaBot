namespace iOSBot.Console.Migrations;

public partial class Device
{
    public string AudienceId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string FriendlyName { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string BuildId { get; set; } = null!;

    public string Product { get; set; } = null!;

    public string BoardId { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Changelog { get; set; } = null!;

    public string Type { get; set; } = null!;

    public uint Color { get; set; }

    public string AssetType { get; set; } = null!;

    public bool Enabled { get; set; }

    public int Priority { get; set; }
}