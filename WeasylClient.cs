﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WeasylLib
{
    public class WeasylClient {
        private string _apiKey;

        public WeasylClient(string apiKey) {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

		public async Task<WeasylGallery> GetUserGalleryAsync(string user, DateTime? since = null, int? count = null, int? folderid = null, int? backid = null, int? nextid = null) {
            if (user == null) throw new ArgumentNullException(nameof(user));

            StringBuilder qs = new StringBuilder();
            if (since != null) qs.Append($"&since={since.Value.ToString("u")}");
            if (count != null) qs.Append($"&count={count}");
            if (folderid != null) qs.Append($"&folderid={folderid}");
            if (backid != null) qs.Append($"&backid={backid}");
            if (nextid != null) qs.Append($"&nextid={nextid}");

            HttpWebRequest req = WebRequest.CreateHttp($"https://www.weasyl.com/api/users/{user}/gallery?{qs}");
            req.Headers["X-Weasyl-API-Key"] = _apiKey;
            using (WebResponse resp = await req.GetResponseAsync())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                string json = await sr.ReadToEndAsync();
                return JsonConvert.DeserializeObject<WeasylGallery>(json);
            }
        }

        public async Task<List<int>> ScrapeCharacterIdsAsync(string user) {
            if (user == null) throw new ArgumentNullException(nameof(user));

            HttpWebRequest req = WebRequest.CreateHttp($"https://www.weasyl.com/characters/{user}");
            using (WebResponse resp = await req.GetResponseAsync())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                string html = await sr.ReadToEndAsync();
                int lastIndex = 0;
                List<int> ids = new List<int>();
                while ((lastIndex = html.IndexOf("\"/character/", lastIndex)) != -1) {
                    lastIndex += "\"/character/".Length;
                    int id = 0;
                    while (true) {
                        char c = html[lastIndex];
                        if (c < '0' || c > '9') break;
                        id = (10 * id) + (c - '0');
                        lastIndex++;
                    }
                    if (id != 0 && !ids.Contains(id)) ids.Add(id);
                }
                return ids;
            }
        }
        
		public async Task<WeasylSubmissionDetail> GetSubmissionAsync(int submitid) {
            HttpWebRequest req = WebRequest.CreateHttp($"https://www.weasyl.com/api/submissions/{submitid}/view");
            req.Headers["X-Weasyl-API-Key"] = _apiKey;
            using (WebResponse resp = await req.GetResponseAsync())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                string json = await sr.ReadToEndAsync();
                return JsonConvert.DeserializeObject<WeasylSubmissionDetail>(json);
            }
        }

        public async Task<WeasylCharacterDetail> GetCharacterAsync(int charid) {
            HttpWebRequest req = WebRequest.CreateHttp($"https://www.weasyl.com/api/characters/{charid}/view");
            req.Headers["X-Weasyl-API-Key"] = _apiKey;
            using (WebResponse resp = await req.GetResponseAsync())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                string json = await sr.ReadToEndAsync();
                return JsonConvert.DeserializeObject<WeasylCharacterDetail>(json);
            }
        }

        public async Task<string> GetAvatarUrlAsync(string username) {
            HttpWebRequest req = WebRequest.CreateHttp($"https://www.weasyl.com/api/useravatar?username={WebUtility.UrlEncode(username)}");
            req.Headers["X-Weasyl-API-Key"] = _apiKey;
            using (WebResponse resp = await req.GetResponseAsync())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                string json = await sr.ReadToEndAsync();
                var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return obj["avatar"];
            }
        }
        
        public async Task<WeasylUser> WhoamiAsync() {
            HttpWebRequest req = WebRequest.CreateHttp("https://www.weasyl.com/api/whoami");
            req.Headers["X-Weasyl-API-Key"] = _apiKey;
            using (WebResponse resp = await req.GetResponseAsync())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                string json = await sr.ReadToEndAsync();
                return JsonConvert.DeserializeObject<WeasylUser>(json);
            }
		}
    }
}
