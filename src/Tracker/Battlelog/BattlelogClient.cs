using Battlelog.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Battlelog
{
    public static class BattlelogClient
    {
        /// <summary>
        ///     Get post check sum used in some battlelog queries
        /// </summary>
        /// <returns></returns>
        public static string GetPostCheckSum()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://battlelog.battlefield.com/bf4/");
                request.CookieContainer = new CookieContainer();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var postChecksum = request.CookieContainer.GetCookies(response.ResponseUri)["beaker.session.id"].Value.Substring(0, 10);

                return postChecksum;
            }
            catch (Exception e)
            {
                //Handle exceptions here however you want
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        /// <summary>
        ///     Get the persona for the given soldierName
        /// </summary>
        /// <param name="soldierName"></param>
        /// <param name="postChecksum"></param>
        /// <returns></returns>
        public static Persona GetPersona(string soldierName, string postChecksum = null)
        {
            using var webClient = new GZipWebClient();

            var data = new NameValueCollection() {
                { "query", soldierName },
                { "post-check-sum", string.IsNullOrWhiteSpace(postChecksum) ? GetPostCheckSum() : postChecksum }
            };

            string searchResults = Encoding.UTF8.GetString(webClient.UploadValues($"https://battlelog.battlefield.com/bf4/search/query/", data));

            var res = JsonConvert.DeserializeObject<Response<Persona[]>>(searchResults);
            if (res.Type.Equals("success") && res.Message.Equals("RESULT") && res.Data.Length > 0)
            {
                var matches = res.Data
                    .Where(x => x.Namespace.Equals("cem_ea_id") && x.Games.Any(x => (x.Value & 2048 /* 2048 is BF4 */) > 0) && x.PersonaName.Equals(soldierName, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(p => p.PersonaId).Select(grp => grp.FirstOrDefault());
                if (matches.Count() == 1) return matches.FirstOrDefault();

                if (matches.Count() > 1)
                {
                    throw new Exception($"An error occured while searching. Found multiple matches.");
                }
                else
                {
                    throw new Exception($"An error occured while searching. Found no matches.");
                }
            }

            return null;
        }

        /// <summary>
        ///     Get the ingame metadata for the personaId
        /// </summary>
        /// <param name="personaId"></param>
        /// <returns></returns>
        public static IngameMetadata GetIngameMetadata(string personaId)
        {
            try
            {
                using var webClient = new GZipWebClient();
                string result = webClient.DownloadString($"https://battlelog.battlefield.com/api/bf4/pc/persona/1/{personaId}/ingame_metadata");

                var res = JsonConvert.DeserializeObject<IngameMetadata>(result);

                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return new IngameMetadata();
            }
        }

        /// <summary>
        ///     Get the server show data
        /// </summary>
        /// <param name="guid">Server guid</param>
        /// <param name="platform">Platform, usually pc</param>
        /// <returns></returns>
        public static dynamic GetServerShow(string guid, string platform = "pc")
        {
            try
            {
                using var webClient = new GZipWebClient();
                string result = webClient.DownloadString($"https://battlelog.battlefield.com/bf4/servers/show/{platform}/{guid}/SERVER/?json=1");

                JObject response = JObject.Parse(result);
                if (!response.TryGetValue("type", out var type))
                    throw new Exception("Request failed");

                if (!response.TryGetValue("message", out var message))
                    throw new Exception("message didn't exist");

                if (!message.ToObject<JObject>().TryGetValue("SERVER_INFO", out var serverInfo))
                    throw new Exception("SERVER_INFO didn't exist");

                return serverInfo.ToObject<dynamic>();
            }
            catch (Exception e)
            {
                //Handle exceptions here however you want
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        /// <summary>
        ///     Get the server snapshot
        /// </summary>
        /// <param name="guid">Server guid</param>
        /// <returns></returns>
        public static Snapshot GetServerSnapshot(string guid)
        {
            try
            {
                using var webClient = new GZipWebClient();
                string result = webClient.DownloadString($"https://keeper.battlelog.com/snapshot/{guid}");
                return JsonConvert.DeserializeObject<ServerInfo>(result)?
                    .Snapshot;
            }
            catch (Exception e)
            {
                //Handle exceptions here however you want
                Console.WriteLine(e.ToString());
                return null;
            }
        }
    }
}
