using Newtonsoft.Json;

namespace Shared.Models
{
    public class User
    {
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("gravatarMd5")]
        public string GravatarMd5 { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("createdAt")]
        public int CreatedAt { get; set; }
        [JsonProperty("presence")]
        public Presence Presence { get; set; }
    }
}
