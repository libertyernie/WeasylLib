using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WeasylLib.Example {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("You can get a Weasyl API key from: https://www.weasyl.com/control/apikeys");
            Console.Write("Enter your API key: ");
            string apiKey = Console.ReadLine();
            if (string.IsNullOrEmpty(apiKey)) return;

            var client = new WeasylClient(apiKey);
			ListGallery(client).GetAwaiter().GetResult();
        }

        static async Task PrintAvatar(WeasylClient client) {
            var user = await client.WhoamiAsync();
            string url = await client.GetAvatarUrlAsync(user.login);
            var request = WebRequest.Create(url);
            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream()) {
                if (Image.FromStream(stream) is Bitmap bmp) {
                    ConsoleImage.ConsoleWriteImage(bmp);
                }
            }
        }

        static async Task ListGallery(WeasylClient client) {
            var user = await client.WhoamiAsync();
            Console.WriteLine(user.login);

            Console.WriteLine("----------");
            var gallery = await client.GetUserGalleryAsync(user.login, count: 1);
            foreach (var s in gallery.submissions) Console.WriteLine(s.title);

            Console.WriteLine("----------");
            gallery = await client.GetUserGalleryAsync(user.login, count: 10, nextid: gallery.nextid);
            foreach (var s in gallery.submissions) Console.WriteLine(s.title);

            Console.WriteLine("----------");
            gallery = await client.GetUserGalleryAsync(user.login, count: 3, nextid: gallery.nextid);
            foreach (var s in gallery.submissions) Console.WriteLine(s.title);

            Console.WriteLine("----------");
            foreach (var s in gallery.submissions) {
                var details = await client.GetSubmissionAsync(s.submitid);
                Console.WriteLine(details.title + ":");
                Console.WriteLine(details.description);
                Console.WriteLine();
            }
        }

        static async Task ListCharacters(WeasylClient client) {
            var user = await client.WhoamiAsync();
            Console.WriteLine(user.login);

            Console.WriteLine("----------");
            var charids = await client.ScrapeCharacterIdsAsync(user.login);
            foreach (int id in charids) Console.WriteLine(id);

            Console.WriteLine("----------");
            foreach (int id in charids.Take(3)) {
                var details = await client.GetCharacterAsync(id);
                Console.WriteLine(details.title);
                Console.WriteLine("Species: " + details.species);
                Console.WriteLine();
            }
        }
    }
}
