using System.Collections.Generic;
using System.Linq;

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

        public override string ToString()
        {
            var value = $"AppID: {AppId}\n" +
                         $"Language: {Language}\n" +
                         $"UnlockAll: {UnlockAll}\n" +
                         $"ExtraProtection: {ExtraProtection}\n" +
                         $"ForceOffline: {ForceOffline}\n" +
                         $"DLC ({DlcList.Count}):\n[\n";
            if (DlcList.Count > 0)
                value = DlcList.Aggregate(value, (current, x) => current + $"  {x.AppId}={x.Name},\n");
            value += "]";
            return value;
        }
    }

    public sealed class CreamConfigModel
    {
    }
}