namespace iOSBot.Service;

public class MqUpdate
{
    public DateTime ReleaseDate { get; set; }
    public string Version { get; set; }
    public string Build { get; set; }
    public string Size { get; set; }
    public Guid TrackId { get; set; }
    public string TrackName { get; set; }
    public string ReleaseType { get; set; }
    public uint Color { get; set; }
}