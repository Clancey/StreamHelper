using System;
using System.Linq;
using System.Threading.Tasks;
using SimpleAuth;
using System.IO;
using SimpleAuth.Providers;
using System.Collections.Generic;

namespace StreamHelper
{
    class Program
    {
        static void Main(string[] args)
        {
		
            Console.WriteLine("Hello World!");
			args?.ToList ()?.ForEach(Console.WriteLine);
            Resolver.Register<IAuthStorage, AuthStorage>();
			var message = "Adding Auto Tweeting to Twitch Markers";
			SendMessage (message).Wait();
			UpdateTopic (message);
			TweetUpdate (message).Wait ();
			Console.WriteLine ("Done!");

            
        }

        static async Task  SendMessage(string message)
		{
			OAuthApi.ShowAuthenticator = async (auth) => {

				var authenticator = new OAuthController (auth);
				await authenticator.GetCredentials (auth.Title);
			};

			var api = new TwitchApi ("Twitch", ApiConstants.TwitchApiKey, ApiConstants.TwitchSecret) {
				Scopes = new [] {
					"clips:edit",
					"user:edit",
					"user:edit:broadcast",
				},
			};
			try {
				var response = await api.UpdateStreamMarker (message);
				Console.WriteLine (response);
			}
			catch(Exception ex) {
				Console.WriteLine (ex);
			}
		}

		static void UpdateTopic(string message)
		{
			const string path = "/Users/clancey/Dropbox/LiveStream/CurrentTopic.txt";
			File.WriteAllText (path, message);
		}

		static async Task TweetUpdate(string message)
		{
			var api = new TwitterApi ("twitter", ApiConstants.TwitterApiKey, ApiConstants.TwitterSecret) {
				RedirectUrl = new Uri("http://localhost"),
			};

			var resp = await api.Post (null, "statuses/update.json", new Dictionary<string, string> {
				["status"] = $"{message} follow along at twitch.tv/Clancey"
			});
			Console.WriteLine (resp);
		}
    }
}
