using Battlelog;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.Redis;
using System;

namespace Tracker.Services
{
    public interface IPersonaService
    {
        public PersonaInfo GetPersonaInfo(string eaGuid, string soldierName, string serverGuid, bool isAuthenticated = false);
        public Persona GetPersona(string soldierName, string serverGuid);
        public IngameMetadata GetIngameMetadata(ulong personaId);
    }

    public class PersonaService : IPersonaService
    {
        private readonly ILogger<PersonaService> _logger;
        private readonly IDistributedCache _distributedCache;
        private Snapshot _snapshot;

        public PersonaService(ILogger<PersonaService> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public PersonaInfo GetPersonaInfo(string eaGuid, string soldierName, string serverGuid, bool isAuthenticated = false)
        {
            var cacheKey = IsValidEAGUID(eaGuid) ? $":eausers:{eaGuid}" : null;

            // Search from cache
            if (cacheKey != null && TryGetPersonaInfo(cacheKey, out var personaInfo))
            {
                // TODO: Update the user clan tag.
                return personaInfo;
            }

            // TODO: Add support for searching from the gameserver directly without requesting snapshot from Battlelog

            // Search from the keeper if server guid was provided
            if (IsValidServerGUID(serverGuid) && TryGetPersonaInfoFromSnapshot(soldierName, serverGuid, out personaInfo))
            {
                if (isAuthenticated && cacheKey != null)
                {
                    AddOrRefreshPersonaInfo(cacheKey, personaInfo);
                }
                return personaInfo;
            }

            // Search from Battlelog search feature
            if (TryGetPersonaInfoFromSearch(soldierName, out personaInfo))
            {
                if (isAuthenticated && cacheKey != null)
                {
                    AddOrRefreshPersonaInfo(cacheKey, personaInfo);
                }
                return personaInfo;
            }

            return null;
        }

        public Persona GetPersona(string soldierName, string serverGuid)
        {
            // Search from Battlelog search feature
            if (TryGetPersonaFromSearch(soldierName, out var persona))
            {
                return persona;
            }

            // TODO: Add support for searching from the gameserver directly without requesting snapshot from Battlelog

            // Search from the keeper if server guid was provided
            if (IsValidServerGUID(serverGuid) && TryGetPersonaInfoFromSnapshot(soldierName, serverGuid, out var personaInfo))
            {
                return new Persona {
                    PersonaId = personaInfo.PersonaId.ToString(),
                    PersonaName = personaInfo.Name,
                    ClanTag = personaInfo.Tag,
                    User = new User {
                        Username = personaInfo.Name
                    }
                };
            }

            return null;
        }

        public IngameMetadata GetIngameMetadata(ulong personaId)
        {
            return BattlelogClient.GetIngameMetadata(personaId.ToString());
        }

        private bool TryGetPersonaInfoFromSnapshot(string soldierName, string serverGuid, out PersonaInfo personaInfo)
        {
            try
            {
                Player player = null;
                if (_snapshot == null || (player = _snapshot.GetPlayerBySoldierName(soldierName)) == null)
                {
                    _snapshot = BattlelogClient.GetServerSnapshot(serverGuid);
                }

                if (player == null && _snapshot != null)
                {
                    player = _snapshot.GetPlayerBySoldierName(soldierName);
                }

                personaInfo = new PersonaInfo {
                    Name = soldierName,
                    PersonaId = player?.PersonaId ?? 0,
                    Tag = player?.Tag
                };

                return player != null;
            }
            catch (Exception ex)
            {
                personaInfo = null;
                _logger.LogError(ex, "Exception getting the personaInfo from snapshot.");

                return false;
            }
        }

        private bool TryGetPersonaFromSearch(string soldierName, out Persona persona)
        {
            try
            {
                var postCheckSum = PostCheckSum;

                if (string.IsNullOrEmpty(postCheckSum))
                {
                    postCheckSum = BattlelogClient.GetPostCheckSum();
                    PostCheckSum = postCheckSum;
                }

                if (string.IsNullOrEmpty(postCheckSum))
                {
                    persona = null;
                    _logger.LogWarning("Unable to get persona from battlelog search because post-check-sum couldn't be retrieved.");

                    return false;
                }

                persona = BattlelogClient.GetPersona(soldierName);

                if (persona == null)
                {
                    _logger.LogWarning("Exception getting the persona from battlelog search.");

                    return false;
                }

                return persona != null;
            }
            catch (Exception ex)
            {
                persona = null;
                _logger.LogError(ex, "Exception getting the persona from battlelog search.");

                return false;
            }
        }

        private bool TryGetPersonaInfoFromSearch(string soldierName, out PersonaInfo personaInfo)
        {
            try
            {
                var postCheckSum = PostCheckSum;

                if (string.IsNullOrEmpty(postCheckSum))
                {
                    postCheckSum = BattlelogClient.GetPostCheckSum();
                    PostCheckSum = postCheckSum;
                }

                if (string.IsNullOrEmpty(postCheckSum))
                {
                    personaInfo = null;
                    _logger.LogWarning("Unable to get personaInfo from battlelog search because post-check-sum couldn't be retrieved.");

                    return false;
                }

                var persona = BattlelogClient.GetPersona(soldierName);

                if (persona == null)
                {
                    personaInfo = null;
                    _logger.LogWarning("Exception getting the personaInfo from battlelog search.");

                    return false;
                }

                personaInfo = new PersonaInfo {
                    Name = persona?.PersonaName,
                    PersonaId = ulong.Parse(persona.PersonaId),
                    Tag = persona?.ClanTag
                };

                return persona != null;
            }
            catch (Exception ex)
            {
                personaInfo = null;
                _logger.LogError(ex, "Exception getting the personaInfo from snapshot.");

                return false;
            }
        }

        private bool IsValidEAGUID(string eaGuid)
        {
            return eaGuid?.Length == 35 && eaGuid.Substring(0, 3).Equals("EA_");
        }

        private bool IsValidServerGUID(string serverGuid)
        {
            return serverGuid?.Length == 36 && Guid.TryParse(serverGuid, out _);
        }

        private string PostCheckSum
        {
            get {
                try
                {
                    return _distributedCache.GetString(":post-check-sum");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception while trying to fetch post-check-sum from Redis.");

                    return null;
                }
            }
            set {
                try
                {
                    _distributedCache.SetString(":post-check-sum", value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception while trying to add post-check-sum to Redis.");
                }
            }
        }

        private string GetGameIdFromCache(string cacheKey)
        {
            try
            {
                return _distributedCache.GetString(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while trying to fetch gameId from Redis.");

                return null;
            }
        }

        private bool TryGetPersonaInfo(string cacheKey, out PersonaInfo personaInfo)
        {
            try
            {
                personaInfo = _distributedCache.Get(cacheKey)?
                    .FromByteArray<PersonaInfo>();

                return personaInfo != null;
            }
            catch (Exception ex)
            {
                personaInfo = null;
                _logger.LogError(ex, "Exception getting the personaInfo from cache.");

                return false;
            }
        }

        private void AddOrRefreshPersonaInfo(string cacheKey, PersonaInfo personaInfo)
        {
            try
            {
                if (_distributedCache == null) return;

                _distributedCache.Set(cacheKey, personaInfo.ToByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception adding/refreshing the personaInfo.");
            }

        }

        private void RemovePersonaInfo(string cacheKey)
        {
            try
            {
                if (_distributedCache == null) return;

                _distributedCache.Remove(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception removing the personaInfo.");
            }
        }
    }
}
