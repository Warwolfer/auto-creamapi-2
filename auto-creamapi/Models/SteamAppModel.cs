using System.Collections.Generic;
using System.Text.Json.Serialization;
using MvvmCross.ViewModels;

namespace auto_creamapi.Models
{
    public class SteamApp
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public override string ToString()
        {
            //return $"AppId: {AppId}, Name: {Name}";
            return $"{AppId}={Name}";
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