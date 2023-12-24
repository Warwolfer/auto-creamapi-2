using System.Text.Json.Serialization;

namespace auto_creamapi.Utils
{

    public class AvailableArchive
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("archived_snapshots")]
        public ArchivedSnapshot ArchivedSnapshots { get; set; }
    }

    public class ArchivedSnapshot
    {
        [JsonPropertyName("closest")]
        public Closest Closest { get; set; }
    }

    public class Closest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("available")]
        public bool Available { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
    }
}
