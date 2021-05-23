using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Shared.Models
{
    public class ServerInfo
    {
        [JsonProperty("LastUpdated")]
        public long lastUpdated { get; set; }
        [JsonProperty("snapshot")]
        public Snapshot Snapshot { get; set; }

        public ServerInfo() { }
    }
}
