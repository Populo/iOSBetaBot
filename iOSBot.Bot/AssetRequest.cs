﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iOSBot.Bot
{

    public class AssetRequest
    {
        public string AssetAudience { get; set; }
        public int ClientVersion { get; set; }
        public string AssetType { get; set; }
        public string BuildVersion { get; set; }
        public string HWModelStr { get; set; }
        public string ProductType { get; set; }
        public string ProductVersion { get; set; }
    }

}