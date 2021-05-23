using Newtonsoft.Json;
using Shared.Models.GameModes;
using Syroot.BinaryData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Shared.Models
{
    public class Snapshot
    {
        [JsonProperty("gameId")]
        public ulong GameId { get; set; }
        [JsonProperty("gameMode")]
        public string GameMode { get; set; }
        [JsonProperty("mapVariant")]
        public byte MapVariant { get; set; }
        [JsonProperty("currentMap")]
        public string CurrentMap { get; set; }
        [JsonProperty("maxPlayers")]
        public int MaxPlayers { get; set; }
        [JsonProperty("waitingPlayers")]
        public int WaitingPlayers { get; set; }
        [JsonProperty("roundTime")]
        public uint RoundTime { get; set; }
        [JsonProperty("defaultRoundTimeMultiplier")]
        public uint DefaultRoundTimeMultiplier { get; set; }

        [JsonProperty("rush", NullValueHandling = NullValueHandling.Ignore)]
        public Rush Rush { get; set; }


        [JsonProperty("teamInfo", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, TeamInfo> TeamInfo { get; set; }

        public Snapshot() { }

        public Snapshot(byte[] buffer)
        {
            using MemoryStream stream = new(buffer);

            stream.Seek(0x13, SeekOrigin.Begin);
            GameId = stream.ReadUInt64(ByteConverter.Big);
            GameMode = stream.ReadString(StringCoding.ByteCharCount);
            MapVariant = stream.Read1Byte();
            var size = stream.Read1Byte(); // Should be 0x08 for Rush and Squad Rush

            var pos = stream.Position;
            // Game mode specific round status
            switch (GameMode)
            {
                case "RushLarge":
                    if (size != 0x08) break;
                    Rush = new Rush(stream);
                    break;
                default:
                    break;
            }
            stream.Position = pos + size;

            CurrentMap = stream.ReadString(StringCoding.ByteCharCount);
            RoundTime = stream.ReadUInt32(ByteConverter.Big);
            DefaultRoundTimeMultiplier = stream.ReadUInt32(ByteConverter.Big);
            MaxPlayers = stream.Read1Byte();
            WaitingPlayers = stream.Read1Byte();

            var teamsCount = stream.Read1Byte(); // Should be 0x02 for Rush and Squad Rush
            var teamInfoOffset = stream.Position;
            TeamInfo = new Dictionary<string, TeamInfo>();

            if (teamsCount > 0) // If zero, there's no more data
            {
                for (byte i = 0; i < teamsCount + 1; i++)
                {
                    TeamInfo.TryAdd(i.ToString(), new TeamInfo(stream, teamsCount, teamInfoOffset, i));
                }
            }
        }

        public int GetJoiningPlayers()
        {
            try
            {
                return TeamInfo.First(x => x.Key == "0").Value.Players.Count;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public int GetTotalPlayers()
        {
            try
            {
                return TeamInfo.Sum(x => x.Value.Players.Count);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public ulong? GetPersonaId(string soldierName)
        {
            var persona = GetPersona(soldierName);
            if (persona != null)
            {
                return ulong.Parse(persona?.Key);
            }

            return null;
        }

        public Player GetPlayerBySoldierName(string soldierName)
        {
            var persona = GetPersona(soldierName);
            if (persona != null)
            {
                persona.Value.Value.PersonaId = ulong.Parse(persona.Value.Key);
                return persona.Value.Value;
            }

            return null;
        }

        public Player GetPlayer(string personaId)
        {
            var persona = PlayerByPersonaId(personaId);
            if (persona != null)
            {
                persona.Value.Value.PersonaId = ulong.Parse(persona.Value.Key);
                return persona.Value.Value;
            }

            return null;
        }

        private KeyValuePair<string, Player>? PlayerByPersonaId(string personaId)
        {
            try
            {
                return TeamInfo?
                    .SelectMany(x => x.Value.Players)
                    .First(x => x.Key.Equals(personaId));
            }
            catch
            {
                return null;
            }
        }

        private KeyValuePair<string, Player>? GetPersona(string soldierName)
        {
            try
            {
                return TeamInfo?
                .SelectMany(x => x.Value.Players)
                .First(x => x.Value.Name.Equals(soldierName));
            }
            catch
            {
                return null;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("##############################################################################################################");
            sb.AppendLine("#                                            General Information                                             #");
            sb.AppendLine("##############################################################################################################");
            sb.AppendLine($"CurrentMap: {CurrentMap}");
            sb.AppendLine($"DefaultRoundTimeMultiplier: {DefaultRoundTimeMultiplier}");
            sb.AppendLine($"GameId: {GameId}");
            sb.AppendLine($"GameMode: {GameMode}");
            sb.AppendLine($"MapVariant: {MapVariant}");
            sb.AppendLine($"MaxPlayers: {MaxPlayers}");
            sb.AppendLine($"WaitingPlayers: {WaitingPlayers}");
            sb.AppendLine($"RoundTime: {RoundTime}");
            sb.AppendLine();

            if (Rush != null)
                sb.AppendLine(Rush.ToString());
            for (int i = 0; i < TeamInfo.Count; i++)
            {
                var team = TeamInfo[i.ToString()];
                sb.AppendLine("##############################################################################################################");
                sb.AppendLine($"#                                             Team {i} Information                                             #");
                sb.AppendLine("##############################################################################################################");

                sb.AppendLine($"| {"PersonaId",13} | {"Tag",4} | {"Name",30} | {"Rank",4} | {"Score",10} | {"Kills",5} | {"Deaths",6} | {"SquadId",7} | {"Role",4} |");
                sb.AppendLine($"|---------------|------|--------------------------------|------|------------|-------|--------|---------|------|");
                foreach (var player in team.Players)
                {
                    sb.AppendLine(player.Value.ToString());
                }
                sb.AppendLine($"|_______________|______|________________________________|______|____________|_______|________|_________|______|");
                sb.AppendLine();
            }

            Hashtable h = new Hashtable(new Dictionary<string, ulong?> { { "data", 291911755 } });

            return sb.ToString();
        }
    }
}
