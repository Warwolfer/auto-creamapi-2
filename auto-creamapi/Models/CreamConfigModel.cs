using System.Collections.Generic;

namespace auto_creamapi.Models
{
    public class CreamConfig
    {
        public CreamConfig()
        {
            DlcList = new List<SteamApp>();
        }

        public int AppId { get; set; }
        public string Language { get; set; }
        public bool UnlockAll { get; set; }
        public bool ExtraProtection { get; set; }
        public bool ForceOffline { get; set; }
        public List<SteamApp> DlcList { get; set; }
    }

    public sealed class CreamConfigModel
    {
    }
}