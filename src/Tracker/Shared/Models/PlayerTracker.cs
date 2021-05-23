using Newtonsoft.Json;

namespace Shared.Models
{
    public class PlayerTracker
    {
        [JsonProperty("soldierName")]
        public string SoldierName { get; set; }
        [JsonProperty("tag")]
        public string Tag { get; set; }
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}
