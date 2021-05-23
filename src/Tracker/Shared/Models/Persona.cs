using Newtonsoft.Json;
using System.Collections.Generic;

namespace Shared.Models
{
    public class Persona
    {
        [JsonProperty("picture")]
        public string Picture { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("user")]
        public User User { get; set; }
        [JsonProperty("updatedAt")]
        public int UpdatedAt { get; set; }
        [JsonProperty("firstPartyId")]
        public string FirstPartyId { get; set; }
        [JsonProperty("personaId")]
        public string PersonaId { get; set; }
        [JsonProperty("personaName")]
        public string PersonaName { get; set; }
        [JsonProperty("gamesLegacy")]
        public string GamesLegacy { get; set; }
        [JsonProperty("namespace")]
        public string Namespace { get; set; }
        [JsonProperty("gamesJson")]
        public string GamesJson { get; set; }
        [JsonProperty("games")]
        public Dictionary<int, ulong> Games { get; set; }
        [JsonProperty("clanTag")]
        public string ClanTag { get; set; }
    }
}
