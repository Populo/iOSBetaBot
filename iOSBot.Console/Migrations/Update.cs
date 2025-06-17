namespace iOSBot.Console.Migrations;

public partial class Update
{
    public Guid Guid { get; set; }

    public string Version { get; set; } = null!;

    public string Build { get; set; } = null!;

    public string Category { get; set; } = null!;

    public DateTime ReleaseDate { get; set; }

    public string Hash { get; set; } = null!;
}