using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared;
using Shared.DiscordHelpers;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Tracker.DAL;
using Tracker.DAL.Models;
using Tracker.ViewModels;

namespace Tracker.Services
{
    public interface IDiscordService
    {
        void SearchSoldier(SearchSoldierInput model);
        void AddPlayerTracker(AddTrackerInput model);
        void ListPlayerTrackers(GetTrackersInput model);
        void RemovePlayerTracker(RemoveTrackerInput model);

        void OnPlayerJoin(SoldierInput model);
        void OnPlayerLeave(SoldierInput model);
    }

    public class DiscordService : IDiscordService
    {
        private const string DISCORD_API_URL = "https://discord.com/api/v9";
        private readonly ILogger<DiscordService> _logger;
        private readonly TrackerContext _context;
        private readonly IPersonaService _personaService;
        private readonly DiscordWebhookClient _trackerWebhookClient;
        private readonly DiscordWebhookClient _joinWebhookClient;

        public DiscordService(ILogger<DiscordService> logger, TrackerContext context, IPersonaService personaService)
        {
            _logger = logger;
            _context = context;
            _personaService = personaService;
            _trackerWebhookClient = new DiscordWebhookClient(Variables.TRACKER_WEBHOOK_URL);
            if (!string.IsNullOrWhiteSpace(Variables.JOINLEAVE_WEBHOOK_URL))
            {
                _joinWebhookClient = new DiscordWebhookClient(Variables.JOINLEAVE_WEBHOOK_URL);
            }
        }

        public void SearchSoldier(SearchSoldierInput model)
        {
            var persona = _personaService?.GetPersona(model.SoldierName, null);
            if (persona == null || !ulong.TryParse(persona.PersonaId, out var personaId))
            {
                PostWebhookEdit(GenerateError($"Failed to find persona with the given name **{model.SoldierName}**."), model);
                return;
            }

            var ingameMetadata = _personaService.GetIngameMetadata(personaId);
            SearchSoldier(model, persona, ingameMetadata);
        }

        public void AddPlayerTracker(AddTrackerInput model)
        {
            var persona = _personaService?.GetPersona(model.SoldierName, null);
            if (persona == null || !ulong.TryParse(persona.PersonaId, out var personaId))
            {
                PostWebhookEdit(GenerateError($"Failed to find persona with the given name **{model.SoldierName}**."), model);
                return;
            }

            // TODO: Actual adding, fetching data from Battlelog, etc.
            if (!AddOrUpdateTracker(model, persona))
            {
                PostWebhookEdit(GenerateError($"Unknown error in the ${nameof(AddOrUpdateTracker)} method."), model);
                return;
            }

            var ingameMetadata = _personaService.GetIngameMetadata(personaId);
            AckPlayerAdded(model, persona, ingameMetadata);
        }

        public void ListPlayerTrackers(GetTrackersInput model)
        {
            var (trackers, start, end, trackersCount) = GetTrackers(model.Offset);
            AckTrackersList(model, trackers, start, end, trackersCount);
        }

        public void RemovePlayerTracker(RemoveTrackerInput model)
        {
            if (!GetTracker(model.Identifier, out var tracker))
            {
                PostWebhookEdit(GenerateError($"Couldn't find a tracker with the given identifier."), model);
                return;
            }

            if (!RemoveTracker(tracker))
            {
                PostWebhookEdit(GenerateError($"Unknown error in the ${nameof(RemoveTracker)} method."), model);
                return;
            }

            AckPlayerRemoved(model, tracker);
        }

        public void OnPlayerJoin(SoldierInput model)
        {
            OnPlayerJoinLeave(model, isLeave: false);
        }

        public void OnPlayerLeave(SoldierInput model)
        {
            OnPlayerJoinLeave(model, isLeave: true);
        }

        private void OnPlayerJoinLeave(SoldierInput model, bool isLeave = false)
        {
            var personaInfo = _personaService.GetPersonaInfo(model.EAGuid, model.SoldierName, model.ServerGuid, isAuthenticated: true);
            if (personaInfo == null)
            {
                _logger.LogWarning($"Couldn't find personaInfo for {model.ServerName} - {model.ServerGuid} - {model.EAGuid} - {model.SoldierName} in {nameof(OnPlayerJoin)} method.");
                return;
            }

            var persona = _personaService?.GetPersona(model.SoldierName, model.ServerGuid);
            var ingameMetadata = _personaService.GetIngameMetadata(personaInfo.PersonaId);

            if (_joinWebhookClient != null)
            {
                AckPlayerJoinLeave(model, persona, ingameMetadata, isLeave);
            }

            if (GetTrackerByPersonaId(personaInfo.PersonaId, out var tracker))
            {
                AckTrackedPlayerJoinLeave(model, tracker, persona, ingameMetadata, isLeave);
            }
        }

        #region CRUD
        private (List<TrackerEntry> trackers, int start, int end, int totalTrackers) GetTrackers(int offset = 0)
        {
            try
            {
                var trackers = _context.TrackerEntries
                    .AsQueryable()
                    .OrderBy(x => x.Id)
                    .Skip(offset)
                    .Take(25)
                    .ToList();
                return (trackers, offset + 1, offset + trackers.Count, GetTrackersCount());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't fetch the tracking list.");
                return (null, 0, 0, 0);
            }
        }

        private int GetTrackersCount()
        {
            try
            {
                return _context.TrackerEntries.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't fetch the tracking list.");
                return 0;
            }
        }

        private bool GetTracker(int id, out TrackerEntry tracker)
        {
            try
            {
                tracker = _context.TrackerEntries.FirstOrDefault(x => x.Id == id);

                return tracker != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get player from the tracking list.");

                tracker = null;
                return false;
            }
        }

        private bool GetTrackerByPersonaId(ulong personaId, out TrackerEntry tracker)
        {
            try
            {
                tracker = _context.TrackerEntries.FirstOrDefault(x => x.PersonaId == personaId);

                return tracker != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't get player from the tracking list.");

                tracker = null;
                return false;
            }
        }

        private bool AddOrUpdateTracker(AddTrackerInput input, Persona persona)
        {
            try
            {
                var personaId = ulong.Parse(persona.PersonaId);

                if (GetTrackerByPersonaId(personaId, out var tracker))
                {
                    // Update tracker
                    tracker.SoldierName = persona.PersonaName;
                    tracker.Tag = persona.ClanTag;
                    tracker.Reason = input.Reason;
                    _context.Update(tracker);
                }
                else
                {
                    // Add tracker
                    _context.Add(new TrackerEntry {
                        PersonaId = personaId,
                        SoldierName = persona.PersonaName,
                        Tag = persona.ClanTag,
                        Reason = input.Reason
                    });
                }

                return _context.SaveChanges() == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't add or update player in the tracking list.");

                return false;
            }
        }

        private bool RemoveTracker(TrackerEntry tracker)
        {
            try
            {
                // Remove tracker
                _context.Remove(tracker);

                return _context.SaveChanges() == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't remove player from the tracking list.");

                return false;
            }
        }
        #endregion

        #region Ack Webhooks
        private void SearchSoldier(SearchSoldierInput input, Persona persona, IngameMetadata ingameMetadata)
        {
            try
            {
                var embed = new EmbedBuilder {
                    Author = new EmbedAuthorBuilder() {
                        Name = persona.PersonaName,
                        Url = $"https://battlelog.battlefield.com/bf4/user/{persona.PersonaName}/",
                        IconUrl = persona?.User?.GravatarMd5 != null ?
                                $"https://www.gravatar.com/avatar/{persona.User.GravatarMd5}?d=https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png" :
                                "https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png"
                    }
                };

                if (ingameMetadata?.EmblemPngUrl != null)
                {
                    embed.WithThumbnailUrl(ingameMetadata.EmblemPngUrl);
                }

                if (!string.IsNullOrEmpty(persona?.PersonaName))
                {
                    if (persona?.PersonaId != null)
                    {
                        embed.AddField("Battlelog",
                            $"[**{persona.PersonaName}** - Battlelog](https://battlelog.battlefield.com/bf4/soldier/{persona.PersonaName}/stats/{persona?.PersonaId}/pc/)");
                    }
                    else
                    {
                        embed.AddField("Battlelog",
                            $"[**{persona.PersonaName}** - Battlelog](https://battlelog.battlefield.com/bf4/user/{persona.PersonaName}/)");
                    }
                    embed.AddField("247fairplay",
                        $"[**{persona.PersonaName}** - 247FairPlay](https://www.247fairplay.com/CheatDetector/{persona.PersonaName})");
                    if (persona?.PersonaId != null)
                    {
                        embed.AddField("BF4CheatReport",
                            $"[**{persona.PersonaName}** - BF4CheatReport](https://bf4cheatreport.com/?pid={persona?.PersonaId}&uid=&cnt=200&startdate=)");
                        embed.AddField("BF4DB",
                            $"[**{persona.PersonaName}** - BF4DB](https://www.bf4db.com/player/{persona?.PersonaId})");
                    }
                }
                else
                {
                    embed.AddField("Error", "Couldn't parse the playername.");
                }

                embed.Color = new Color(0, 255, 0);

                embed.Footer = new EmbedFooterBuilder() {
                    Text = $"© BattleTracker by xfileFIN ({DateTime.Now.Year})",
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);

                var data = new WebhookModel {
                    Embeds = new WebhookEmbed[] {
                    embed.Build().ToModel()
                }
                };

                PostWebhookEdit(data, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't post searched soldier webhook.");
            }
        }

        private void AckPlayerAdded(AddTrackerInput input, Persona persona, IngameMetadata ingameMetadata)
        {
            try
            {
                var embed = new EmbedBuilder {
                    Title = "Success",
                    Description = "Player added to tracked players list",
                    Author = new EmbedAuthorBuilder() {
                        Name = persona.PersonaName,
                        Url = $"https://battlelog.battlefield.com/bf4/user/{persona.PersonaName}/",
                        IconUrl = persona?.User?.GravatarMd5 != null ?
                                $"https://www.gravatar.com/avatar/{persona.User.GravatarMd5}?d=https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png" :
                                "https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png"
                    }
                };

                if (ingameMetadata?.EmblemPngUrl != null)
                {
                    embed.WithThumbnailUrl(ingameMetadata.EmblemPngUrl);
                }

                embed.AddField("Reason", input.Reason);

                if (!string.IsNullOrEmpty(persona?.PersonaName))
                {
                    if (persona?.PersonaId != null)
                    {
                        embed.AddField("Battlelog",
                            $"[**{persona.PersonaName}** - Battlelog](https://battlelog.battlefield.com/bf4/soldier/{persona.PersonaName}/stats/{persona?.PersonaId}/pc/)");
                    }
                    else
                    {
                        embed.AddField("Battlelog",
                            $"[**{persona.PersonaName}** - Battlelog](https://battlelog.battlefield.com/bf4/user/{persona.PersonaName}/)");
                    }
                    embed.AddField("247fairplay",
                        $"[**{persona.PersonaName}** - 247FairPlay](https://www.247fairplay.com/CheatDetector/{persona.PersonaName})");
                    if (persona?.PersonaId != null)
                    {
                        embed.AddField("BF4CheatReport",
                            $"[**{persona.PersonaName}** - BF4CheatReport](https://bf4cheatreport.com/?pid={persona?.PersonaId}&uid=&cnt=200&startdate=)");
                        embed.AddField("BF4DB",
                            $"[**{persona.PersonaName}** - BF4DB](https://www.bf4db.com/player/{persona?.PersonaId})");
                    }
                }
                else
                {
                    embed.AddField("Error", "Couldn't parse the playername.");
                }

                embed.Color = new Color(0, 255, 0);

                embed.Footer = new EmbedFooterBuilder() {
                    Text = $"© BattleTracker by xfileFIN ({DateTime.Now.Year})",
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);

                var data = new WebhookModel {
                    Embeds = new WebhookEmbed[] {
                        embed.Build().ToModel()
                    }
                };

                PostWebhookEdit(data, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't post the player added webhook.");
            }
        }

        private void AckPlayerJoinLeave(SoldierInput input, Persona persona, IngameMetadata ingameMetadata, bool isLeave = false)
        {
            try
            {
                var embed = new EmbedBuilder {
                    Title = isLeave ? "Player left" : "Player joined",
                    Description = input.ServerName
                };

                if (ingameMetadata?.EmblemPngUrl != null)
                {
                    embed.WithThumbnailUrl(ingameMetadata.EmblemPngUrl);
                }

                if (persona?.PersonaId != null)
                {
                    embed.AddField("Battlelog",
                        $"[**{input.SoldierName}** - Battlelog](https://battlelog.battlefield.com/bf4/soldier/{input.SoldierName}/stats/{persona?.PersonaId}/pc/)");
                }
                else
                {
                    embed.AddField("Battlelog",
                        $"[**{input.SoldierName}** - Battlelog](https://battlelog.battlefield.com/bf4/user/{input.SoldierName}/)");
                }

                embed.Color = isLeave ? new Color(255, 0, 0) : new Color(0, 255, 0);

                embed.Footer = new EmbedFooterBuilder() {
                    Text = $"© BattleTracker by xfileFIN ({DateTime.Now.Year})",
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);

                _joinWebhookClient?.SendMessageAsync(embeds: new List<Embed> { embed.Build() }, username: input.SoldierName, avatarUrl: persona?.User?.GravatarMd5 != null ?
                                    $"https://www.gravatar.com/avatar/{persona.User.GravatarMd5}?d=https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png" :
                                    "https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't post Join or Leave webhook.");
            }
        }

        private void AckTrackedPlayerJoinLeave(SoldierInput input, TrackerEntry tracker, Persona persona, IngameMetadata ingameMetadata, bool isLeave = false)
        {
            try
            {
                var embed = new EmbedBuilder {
                    Title = isLeave ? "Tracked player left" : "Tracked player joined",
                    Description = input.ServerName,
                    //Author = new EmbedAuthorBuilder() {
                    //    Name = persona.PersonaName,
                    //    Url = $"https://battlelog.battlefield.com/bf4/user/{input.SoldierName}/",
                    //    IconUrl = persona?.User?.GravatarMd5 != null ?
                    //                $"https://www.gravatar.com/avatar/{persona.User.GravatarMd5}?d=https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png" :
                    //                "https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png"
                    //}
                };

                embed.AddField("Reason", tracker.Reason);

                if (!isLeave)
                {
                    if (ingameMetadata?.EmblemPngUrl != null)
                    {
                        embed.WithThumbnailUrl(ingameMetadata.EmblemPngUrl);
                    }

                    if (persona?.PersonaId != null)
                    {
                        embed.AddField("Battlelog",
                            $"[**{input.SoldierName}** - Battlelog](https://battlelog.battlefield.com/bf4/soldier/{input.SoldierName}/stats/{persona?.PersonaId}/pc/)");
                    }
                    else
                    {
                        embed.AddField("Battlelog",
                            $"[**{input.SoldierName}** - Battlelog](https://battlelog.battlefield.com/bf4/user/{input.SoldierName}/)");
                    }
                    embed.AddField("247fairplay",
                        $"[**{input.SoldierName}** - 247FairPlay](https://www.247fairplay.com/CheatDetector/{input.SoldierName})");
                    if (persona?.PersonaId != null)
                    {
                        embed.AddField("BF4CheatReport",
                            $"[**{input.SoldierName}** - BF4CheatReport](https://bf4cheatreport.com/?pid={persona?.PersonaId}&uid=&cnt=200&startdate=)");
                        embed.AddField("BF4DB",
                            $"[**{input.SoldierName}** - BF4DB](https://www.bf4db.com/player/{persona?.PersonaId})");
                    }
                }

                embed.Color = isLeave ? new Color(255, 0, 0) : new Color(0, 255, 0);

                embed.Footer = new EmbedFooterBuilder() {
                    Text = $"© BattleTracker by xfileFIN ({DateTime.Now.Year})",
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);

                _trackerWebhookClient.SendMessageAsync(isLeave ? null : "@here", embeds: new List<Embed> { embed.Build() }, username: input.SoldierName, avatarUrl: persona?.User?.GravatarMd5 != null ?
                                    $"https://www.gravatar.com/avatar/{persona.User.GravatarMd5}?d=https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png" :
                                    "https://eaassets-a.akamaihd.net/battlelog/defaultavatars/default-avatar-36.png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't post tracked player Join or Leave webhook.");
            }
        }

        private void AckTrackersList(InteractionInput model, List<TrackerEntry> trackers, int start, int end, int trackersTotal)
        {
            try
            {
                var embed = new EmbedBuilder {
                    Title = $"Showing {start} - {end} of {trackersTotal} tracked players",
                    Description = (end < trackersTotal) ? $"Use /gettrackers offset:{end}" : null
                };

                foreach (var tracker in trackers)
                {
                    embed.AddField($"{tracker.SoldierName} (ID: {tracker.Id})", $"[{tracker.Reason}](https://battlelog.battlefield.com/bf4/soldier/{tracker?.SoldierName}/stats/{tracker?.PersonaId}/pc/)", true);
                }

                embed.Color = new Color(0, 255, 0);

                embed.Footer = new EmbedFooterBuilder() {
                    Text = $"© BattleTracker by xfileFIN ({DateTime.Now.Year})",
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);

                var data = new WebhookModel {
                    Embeds = new WebhookEmbed[] {
                        embed.Build().ToModel()
                    }
                };

                PostWebhookEdit(data, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't post trackers list webhook.");
            }
        }

        private void AckPlayerRemoved(RemoveTrackerInput input, TrackerEntry tracker)
        {
            try
            {
                var embed = new EmbedBuilder {
                    Title = "Success",
                    Description = $"Player **{tracker.SoldierName}** removed from the tracked players list",
                };

                embed.Color = new Color(0, 255, 0);

                embed.Footer = new EmbedFooterBuilder() {
                    Text = $"© BattleTracker by xfileFIN ({DateTime.Now.Year})",
                };

                embed.WithTimestamp(DateTimeOffset.UtcNow);

                var data = new WebhookModel {
                    Embeds = new WebhookEmbed[] {
                        embed.Build().ToModel()
                    }
                };

                PostWebhookEdit(data, input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't post player removed webhook.");
            }
        }
        #endregion

        private static WebhookModel GenerateError(string message)
        {
            var embed = new EmbedBuilder {
                Title = "Error",
                Description = message,
            };
            embed.Color = new Color(255, 0, 0);
            embed.Footer = new EmbedFooterBuilder() {
                Text = $"© BattleTracker by xfileFIN ({DateTime.Now.Year})",
            };

            embed.WithTimestamp(DateTimeOffset.UtcNow);

            return new WebhookModel {
                Embeds = new WebhookEmbed[] {
                    embed.Build().ToModel()
                }
            };
        }

        private static void PostWebhookEdit(WebhookModel data, InteractionInput interaction)
        {
            using var client = new GZipWebClient();

            client.Headers[HttpRequestHeader.ContentType] = "application/json";

            var json = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            });
            _ = client.UploadData($"{DISCORD_API_URL}/webhooks/{interaction.ApplicationId}/{interaction.InteractionToken}/messages/@original",
                "PATCH",
                Encoding.UTF8.GetBytes(json));
        }
    }
}
