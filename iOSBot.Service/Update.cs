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
        public Device Device { get; set; }
        public string ReleaseType { get; set; }
        public int Revision => GetRevision();
        public string Source { get; set; }
        public string VersionReadable => GetReadableVersion();
        public JObject JsonRequest => GetJsonRequest();

        private int GetRevision()
        {
            /*
             * 3 cases:
             *
             * 1. completely new version
             *  - proceed as normal, no revision
             * 2. new build of existing version
             *  - revision + 1
             * 3. same build of same version
             *  - do nothing
             */

            using var db = new BetaContext();
            var dbUpdates = db.Updates
                .Where(u => u.Version.Contains(this.VersionReadable) &&
                            u.Category == this.Group)
                .OrderByDescending(u => u.ReleaseDate);

            // case 1 || 3, short circuit to prevent any kind of npe
            // first update of this version (17.0 beta 8, 17.0 GM, etc)
            if (!dbUpdates.Any() ||
                dbUpdates.Any(u => u.Build == this.Build &&
                                   this.ReleaseDate == u.ReleaseDate)) return 0;

            // case 2
            // attempt to prevent double counting releases in the situation where it detects
            // update but then immediately after detects the old version because of apple server stuff 
            if (dbUpdates.Any(u => u.ReleaseDate.Date != DateTime.Today.Date))
            {
                return dbUpdates.Count();
            }

            return 0;
        }

        private string GetReadableVersion()
        {
            string majorVersion = Version.Replace("9.9.", "");
            bool isBeta = ReleaseType == "beta";
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