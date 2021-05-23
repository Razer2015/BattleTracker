using System.ComponentModel.DataAnnotations;

namespace Tracker.DAL.Models
{
    public class TrackerEntry
    {
        [Key]
        public int Id { get; set; }

        public ulong PersonaId { get; set; }
        public string SoldierName { get; set; }
        public string Tag { get; set; }
        public string Reason { get; set; }
    }
}
