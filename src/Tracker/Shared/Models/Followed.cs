namespace Shared.Models {
    public class Followed {
        public ulong DiscordUserId { get; set; }
        public string EAUserName { get; set; }
        public string EAUserId { get; set; }
        public string PersonaId { get; set; }
        public string Reason { get; set; }
    }
}
