using Newtonsoft.Json;

namespace Shared.Models
{
    public class BattlelogPlayerCountsViewModel
    {
        [JsonProperty("map")]
        public string Map { get; set; }
        [JsonProperty("mapMode")]
        public ulong MapMode { get; set; }
        [JsonProperty("players")]
        public ushort Players { get; set; }
        [JsonProperty("queued")]
        public ushort Queued { get; set; }
        [JsonProperty("slots")]
        public BattlelogSlotTypesViewModel Slots { get; set; }
    }

    public class BattlelogSlotTypesViewModel
    {
        [JsonProperty("1")]
        public BattlelogSlotsViewModel Queue { get; set; }
        [JsonProperty("2")]
        public BattlelogSlotsViewModel Soldier { get; set; }
        [JsonProperty("8")]
        public BattlelogSlotsViewModel Spectator { get; set; }
    }

    public class BattlelogSlotsViewModel
    {
        [JsonProperty("current")]
        public ushort Current { get; set; }
        [JsonProperty("max")]
        public ushort Max { get; set; }
    }
}
