using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iOSBot.Bot
{
    internal class Update
    {
        public DateTime ReleaseDate { get; set; }
        public string Group { get; set; }
        public string Version { get; set; }
        public string VersionDocId { get; set; }
        public string Build { get; set; }
        public long SizeBytes { get; set; }
        public Device Device { get; set; }
        public ReleaseType ReleaseType { get; set; }
        public string VersionReadable => GetReadableVersion();
        public string Size => GetReadableSize();
        public string ChangelogVersion => GetChangelogVersion();

        private string GetChangelogVersion()
        {
            var parts = Version.Split('.');
            string version = "";
            if (parts.Length > 0)
            {
                version += parts[0];
            }
            if (parts.Length > 1)
            {
                if (parts[1] != "0") version += "." + parts[1];
            }
            return version;
        }

        private string GetReadableVersion()
        {
            string majorVersion = Version.Replace("9.9.", "");
            bool isBeta = VersionDocId.Contains("beta", StringComparison.CurrentCultureIgnoreCase);
            string betaNumber = VersionDocId.Split("Beta").Last();
            string releaseType = ReleaseType == ReleaseType.DEVBETA ? "Developer" : "Public";
            if (ReleaseType != ReleaseType.RELEASE)
            {
                if (VersionDocId.Contains("short", StringComparison.CurrentCultureIgnoreCase))
                {
                    majorVersion += " Release Candidate";
                }
            }

            return !isBeta ? majorVersion : $"{majorVersion} {releaseType} Beta {betaNumber}";
        }

        private string GetReadableSize()
        {
            string[] units = new[] { "B", "KB", "MB", "GB", "TB" }; // should never be bigger than gb but you know

            int i = 0;
            decimal size = SizeBytes;

            while(size > 1000)
            {
                ++i;
                size /= 1000;
            }

            return $"{Math.Round(size, 2)} {units[i]}";
        }
    }
}
