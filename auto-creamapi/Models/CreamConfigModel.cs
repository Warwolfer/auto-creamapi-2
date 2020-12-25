using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using auto_creamapi.Utils;
using IniParser;
using IniParser.Model;

namespace auto_creamapi.Models
{
    public class CreamConfig
    {
        public int AppId { get; set; }
        public string Language { get; set; }
        public bool UnlockAll { get; set; }
        public bool ExtraProtection { get; set; }
        public bool ForceOffline { get; set; }
        public List<SteamApp> DlcList { get; set; }

        public CreamConfig()
        {
            DlcList = new List<SteamApp>();
        }
    }
    public sealed class CreamConfigModel
    {
        
    }
}
