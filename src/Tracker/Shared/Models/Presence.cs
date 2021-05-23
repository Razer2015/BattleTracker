using Newtonsoft.Json;

namespace Shared.Models
{
    public class Presence
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("updatedAt")]
        public int UpdatedAt { get; set; }
        [JsonProperty("presenceStates")]
        public string PresenceStates { get; set; }
    }
}
