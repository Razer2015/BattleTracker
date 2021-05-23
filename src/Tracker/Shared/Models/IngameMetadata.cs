using Newtonsoft.Json;

namespace Shared.Models
{
    public class IngameMetadata
    {
        [JsonProperty("clubRank")]
        public string ClubRank { get; set; }
        [JsonProperty("personaId")]
        public string PersonaId { get; set; }
        [JsonProperty("emblemUrl")]
        public string EmblemUrl { get; set; }
        [JsonProperty("clubName")]
        public string ClubName { get; set; }
        [JsonProperty("countryCode")]
        public string CountryCode { get; set; }

        public string EmblemPngUrl => EmblemUrl?.Replace(".dds", ".png").Replace("http://", "https://");
    }
}
