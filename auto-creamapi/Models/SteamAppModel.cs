using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using auto_creamapi.Utils;

namespace auto_creamapi.Models
{
    public class SteamApp
    {
        private string _name;
        private string _comparableName;
        [JsonPropertyName("appid")] public int AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _comparableName = Regex.Replace(value, Misc.SpecialCharsRegex, "").ToLower();
            }
        }

        public bool CompareName(string value)
        {
            return _comparableName.Equals(value);
        }

        public override string ToString()
        {
            return $"{AppId}={Name}";
        }
    }

    public class AppList
    {
        [JsonPropertyName("apps")] public List<SteamApp> Apps { get; set; }
    }

    public class SteamApps
    {
        [JsonPropertyName("applist")] public AppList AppList { get; set; }
    }
}