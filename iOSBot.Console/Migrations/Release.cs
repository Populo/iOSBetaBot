namespace iOSBot.Console.Migrations;

public partial class Release
{
    public Guid Id { get; set; }

    public string Major { get; set; } = null!;

    public string Minor { get; set; } = null!;

    public string Beta { get; set; } = null!;

    public DateTime Date { get; set; }

    public int WaitTime { get; set; }
}