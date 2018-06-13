using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WeasylLib.Frontend {
	public class WeasylFrontendClient {
		private CookieContainer cookieContainer = new CookieContainer();

		public string WZL {
			get {
				return cookieContainer.GetCookies(new Uri("https://www.weasyl.com"))["WZL"]?.Value;
			}
			set {
				cookieContainer.SetCookies(new Uri("https://www.weasyl.com"), "WZL=" + Regex.Replace(value, "[^A-Za-z0-9]", ""));
			}
		}

		private HttpWebRequest CreateRequest(string url) {
			HttpWebRequest req = WebRequest.CreateHttp(url);
			req.CookieContainer = cookieContainer;
			req.UserAgent = "WeasylLib.Frontend/0.1 (https://https://github.com/libertyernie/WeasylLib)";
			req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
			return req;
		}

		private async Task<string> GetCsrfTokenAsync(string url) {
			Regex regex = new Regex("<html[^>]* data-csrf-token=['\"]([A-Za-z0-9]+)['\"]");

			HttpWebRequest req = CreateRequest(url);
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

			HttpWebRequest req = CreateRequest($"https://www.weasyl.com/signin");
			req.Method = "POST";
			req.ContentType = "application/x-www-form-urlencoded";
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

			HttpWebRequest req = CreateRequest("https://www.weasyl.com/");
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

			HttpWebRequest req = CreateRequest("https://www.weasyl.com/");
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

		public struct Folder {
			public int FolderId;
			public string Name;

			public override string ToString() {
				return Name;
			}
		}

		public async Task<IEnumerable<Folder>> GetFoldersAsync() {
			var list = new List<Folder>();

			HttpWebRequest req = CreateRequest("https://www.weasyl.com/submit/visual");
			using (WebResponse resp = await req.GetResponseAsync())
			using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
				string line;
				while ((line = await sr.ReadLineAsync()) != null) {
					if (line.Contains("<select name=\"folderid\"")) {
						break;
					}
				}

				var regex = new Regex(@"<option value=""(\d+)"">([^<]+)</option>");
				while ((line = await sr.ReadLineAsync()) != null) {
					var match = regex.Match(line);
					if (match.Success && int.TryParse(match.Groups[1].Value, out int id)) {
						list.Add(new Folder {
							FolderId = id,
							Name = match.Groups[2].Value
						});
					}
					if (line.Contains("</select>")) break;
				}
			}

			return list;
		}

		public enum SubmissionType {
			Sketch = 1010,
			Traditional = 1020,
			Digital = 1030,
			Animation = 1040,
			Photography = 1050,
			Design_Interface = 1060,
			Modeling_Sculpture = 1070,
			Crafts_Jewelry = 1075,
			Sewing_Knitting = 1078,
			Desktop_Wallpaper = 1080,
			Other = 1999,
		}

		public enum Rating {
			General = 10,
			Mature = 30,
			Explicit = 40,
		}

		public async Task<Uri> UploadVisualAsync(byte[] data, string title, SubmissionType subtype, int? folderid, Rating rating, string content, IEnumerable<string> tags) {
			string token = await GetCsrfTokenAsync("https://www.weasyl.com/submit/visual");

			string boundary = "--------------------" + Guid.NewGuid();

			HttpWebRequest req = CreateRequest("https://www.weasyl.com/submit/visual");
			req.Method = "POST";
			req.ContentType = $"multipart/form-data; boundary={boundary}";
			using (Stream stream = await req.GetRequestStreamAsync())
			using (StreamWriter sw = new StreamWriter(stream)) {
				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync("Content-Disposition: form-data; name=\"token\"");
				sw.WriteLine();
				sw.WriteLine(token);

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync($"Content-Disposition: form-data; name=\"submitfile\"; filename=\"picture.dat\"");
				sw.WriteLine();
				sw.Flush();
				stream.Write(data, 0, data.Length);
				stream.Flush();
				sw.WriteLine();

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync($"Content-Disposition: form-data; name=\"thumbfile\"; filename=\"\"");
				sw.WriteLine();
				sw.WriteLine();

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync("Content-Disposition: form-data; name=\"title\"");
				sw.WriteLine();
				sw.WriteLine(title);

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync("Content-Disposition: form-data; name=\"subtype\"");
				sw.WriteLine();
				sw.WriteLine((int)subtype);

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync("Content-Disposition: form-data; name=\"folderid\"");
				sw.WriteLine();
				sw.WriteLine(folderid);

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync("Content-Disposition: form-data; name=\"rating\"");
				sw.WriteLine();
				sw.WriteLine((int)rating);

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync("Content-Disposition: form-data; name=\"content\"");
				sw.WriteLine();
				sw.WriteLine(content);

				await sw.WriteLineAsync("--" + boundary);
				await sw.WriteLineAsync("Content-Disposition: form-data; name=\"tags\"");
				sw.WriteLine();
				sw.WriteLine(string.Join(" ", tags.Select(s => s.Replace(' ', '_'))));

				await sw.WriteLineAsync("--" + boundary + "--");
			}
			using (WebResponse resp = await req.GetResponseAsync()) {
				return resp.ResponseUri;
			}
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
