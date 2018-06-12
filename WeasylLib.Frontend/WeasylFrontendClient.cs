using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WeasylLib.Frontend {
	public class WeasylFrontendClient {
		private readonly CookieContainer cookieContainer = new CookieContainer();

		private async Task<string> GetCsrfTokenAsync(string url) {
			Regex regex = new Regex("<html[^>]* data-csrf-token=['\"]([A-Za-z0-9]+)['\"]");

			HttpWebRequest req = WebRequest.CreateHttp(url);
			req.CookieContainer = cookieContainer;
			req.UserAgent = "WeasylLib.Frontend/0.1 (https://https://github.com/libertyernie/WeasylLib)";
			using (WebResponse resp = await req.GetResponseAsync())
			using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
				string line;
				while ((line = await sr.ReadLineAsync()) != null) {
					var match = regex.Match(line);
					if (match.Success) {
						return match.Groups[1].Value;
					}
				}
			}

			throw new Exception($"Cross-site request forgery prevention token not found on {req.RequestUri}");
		}

		public async Task SignInAsync(string username, string password, bool sfw = false) {
			string token = await GetCsrfTokenAsync("https://www.weasyl.com/signin");

			HttpWebRequest req = WebRequest.CreateHttp($"https://www.weasyl.com/signin");
			req.CookieContainer = cookieContainer;
			req.Method = "POST";
			req.ContentType = "application/x-www-form-urlencoded";
			req.UserAgent = "WeasylLib.Frontend/0.1 (https://https://github.com/libertyernie/WeasylLib)";
			using (var sw = new StreamWriter(await req.GetRequestStreamAsync())) {
				await sw.WriteAsync($"token={token}&");
				await sw.WriteAsync($"username={WebUtility.UrlEncode(username)}&");
				await sw.WriteAsync($"password={WebUtility.UrlEncode(password)}&");
				await sw.WriteAsync($"sfwmode={(sfw ? "sfw" : "nsfw")}&");
				await sw.WriteAsync("referer=https://www.weasyl.com/");
			}
			using (WebResponse resp = await req.GetResponseAsync()) { }
		}

		private async Task<Uri> GetSignOutUrlAsync() {
			Regex regex = new Regex("/signout\\?token=[A-Za-z0-9]+");

			HttpWebRequest req = WebRequest.CreateHttp("https://www.weasyl.com/");
			req.CookieContainer = cookieContainer;
			req.UserAgent = "WeasylLib.Frontend/0.1 (https://https://github.com/libertyernie/WeasylLib)";
			using (WebResponse resp = await req.GetResponseAsync())
			using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
				string line;
				while ((line = await sr.ReadLineAsync()) != null) {
					var match = regex.Match(line);
					if (match.Success) {
						return new Uri(
							new Uri("https://www.weasyl.com/"),
							match.Value);
					}
				}
			}

			return null;
		}

		public async Task<string> GetUsernameAsync() {
			Regex regex = new Regex("<a id=['\"]username['\"][^>]* href=['\"]/~([^'\"]+)");

			HttpWebRequest req = WebRequest.CreateHttp("https://www.weasyl.com/");
			req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
			req.CookieContainer = cookieContainer;
			req.UserAgent = "WeasylLib.Frontend/0.1 (https://https://github.com/libertyernie/WeasylLib)";
			using (WebResponse resp = await req.GetResponseAsync())
			using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
				string line;
				while ((line = await sr.ReadLineAsync()) != null) {
					var match = regex.Match(line);
					if (match.Success) {
						return match.Groups[1].Value;
					}
				}
			}

			return null;
		}

		public async Task SignOutAsync() {
			Uri uri = await GetSignOutUrlAsync();
			if (uri == null) return;

			HttpWebRequest req = WebRequest.CreateHttp(uri);
			req.CookieContainer = cookieContainer;
			req.UserAgent = "WeasylLib.Frontend/0.1 (https://https://github.com/libertyernie/WeasylLib)";
			using (WebResponse resp = await req.GetResponseAsync()) { }
		}
	}
}
