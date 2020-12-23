using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace auto_creamapi.POCOs
{
    public class SteamApp
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"AppId: {AppId}, Name: {Name}";
        }

        public bool CompareId(SteamApp steamApp)
        {
            return AppId.Equals(steamApp.AppId);
        }
    }

    public class AppList
    {
        [JsonPropertyName("apps")]
        public List<SteamApp> Apps { get; set; }
    }

    public class SteamApps
    {
        [JsonPropertyName("applist")]
        public AppList AppList { get; set; }
    }
}