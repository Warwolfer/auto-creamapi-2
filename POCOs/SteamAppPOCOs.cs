using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace auto_creamapi.POCOs
{
    public class App
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"AppId: {AppId}, Name: {Name}";
        }

        public bool CompareId(App app)
        {
            return AppId.Equals(app.AppId);
        }
    }

    public class Applist
    {
        [JsonPropertyName("apps")]
        public List<App> Apps { get; set; }
    }

    public class SteamApps
    {
        [JsonPropertyName("applist")]
        public Applist AppList { get; set; }
    }
}