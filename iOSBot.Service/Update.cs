using iOSBot.Data;
using Newtonsoft.Json.Linq;

namespace iOSBot.Service
{
    public class Update
    {
        public DateTime ReleaseDate { get; set; }
        public string Group { get; set; }
        public string Version { get; set; }
        public string VersionDocId { get; set; }
        public string Build { get; set; }
        public long SizeBytes { get; set; }
        public Device Device { get; set; }
        public string ReleaseType { get; set; }
        public int Revision { get; set; }
        public string VersionReadable => GetReadableVersion();
        public string Size => GetReadableSize();
        public JObject JsonRequest => GetJsonRequest();

        private string GetReadableVersion()
        {
            string majorVersion = Version.Replace("9.9.", "");
            bool isBeta = VersionDocId.Contains("beta", StringComparison.CurrentCultureIgnoreCase);
            string betaNumber = VersionDocId.Split("Beta").Last();
            if (ReleaseType != "Release")
            {
                if (VersionDocId.Contains("short", StringComparison.CurrentCultureIgnoreCase) 
                    || VersionDocId.Contains("rc", StringComparison.CurrentCultureIgnoreCase))
                {
                    majorVersion += " Release Candidate";
                }
                else if (VersionDocId.Contains("long", StringComparison.CurrentCultureIgnoreCase) 
                         || VersionDocId.Contains("gm", StringComparison.CurrentCultureIgnoreCase))
                {
                    majorVersion += " Golden Master";
                }
            }

            string revisionAppend = "";
            if (Revision != 0)
            {
                revisionAppend = " Revision " + Revision;
            }

            return (!isBeta ? majorVersion : $"{majorVersion} {ReleaseType} Beta {betaNumber}") + revisionAppend;
        }

        private string GetReadableSize()
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" }; // should never be bigger than gb but you know

            int i = 0;
            decimal size = SizeBytes;

            while(size > 1000)
            {
                ++i;
                size /= 1000;
            }

            return $"{Math.Round(size, 2)} {units[i]}";
        }

        private JObject GetJsonRequest()
        {
            return new JObject(
                new JProperty("AssetAudience", Device.AudienceId),
                new JProperty("AssetType", Device.AssetType),
                new JProperty("ClientVersion", 2),
                new JProperty("BuildVersion", Device.BuildId),
                new JProperty("HWModelStr", Device.BoardId),
                new JProperty("ProductType", Device.Product),
                new JProperty("ProductVersion", Device.Version));
        }
    }
}
