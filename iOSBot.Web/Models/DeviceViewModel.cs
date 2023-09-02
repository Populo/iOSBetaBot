using iOSBot.Data;

namespace iOSBot.Web.Models
{
    public class DeviceViewModel
    {
        public string AudienceId { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
        public string BuildId { get; set; }
        public string Product { get; set; }
        public string BoardId { get; set; }
        public string Category { get; set; }
        public string Changelog { get; set; }
        // Developer, Public, Release
        public string Type { get; set; }
        public string Color { get; set; }
        public string AssetType { get; set; }
    }
}
