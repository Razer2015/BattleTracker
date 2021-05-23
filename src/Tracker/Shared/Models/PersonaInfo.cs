using Newtonsoft.Json;

namespace Shared.Models
{
    public class PersonaInfo
    {
        [JsonProperty("personaId")]
        public ulong PersonaId { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("tag")]
        public string Tag { get; set; }
    }
}
