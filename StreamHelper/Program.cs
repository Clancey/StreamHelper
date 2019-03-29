using System;
using System.Linq;
using System.Threading.Tasks;
using SimpleAuth;
using System.IO;
using SimpleAuth.Providers;
using System.Collections.Generic;
using Mono.Options;
using System.Diagnostics;
using System.Net.Http;
using System.Web;

namespace StreamHelper {
	class Program {
		static async Task Main (string [] args)
		{
			Resolver.Register<IAuthStorage, AuthStorage> ();
			OAuthApi.ShowAuthenticator = async (auth) => {

				var authenticator = new OAuthController (auth);
				await authenticator.GetCredentials (auth.Title);
			};


			Console.WriteLine ("Welcome to the Stream Helper!");
			//Console.WriteLine ($"Args: {String.Join (" ,", args)}");

			//var s = await UploadPhoto ("https://static-cdn.jtvnw.net/previews-ttv/live_user_clancey-1280x720.jpg");
			//return;


			string message = null;
			bool show_help = false;
			bool shouldLogOut = false;
			var p = new OptionSet () {
				{ "m|message=", "The Message to be tweeted/Twitched",
				  v => message = v },
				{ "p|profile=", "the current profile",
				  (v) => Settings.Profile = v },
				{"f|file=","The file path we will update.",v=> Settings.TextFilePath = v },
				{"logout","log out of all social accounts", v=> shouldLogOut = v != null },
				{ "h|help",  "show this message and exit",
				  v => show_help = v != null },
			};

			List<string> extra;
			try {
				extra = p.Parse (args);

			} catch (Exception ex) {
				Debug.WriteLine (ex);
				ShowHelp (p);
				return;
			}

			if (show_help || (args?.Length ?? 0) == 0) {
				ShowHelp (p);
				return;
			}

			if (shouldLogOut) {
				GetTwitchApi ().Logout ();
				GetTwitterApi ().Logout ();
			}

			if (string.IsNullOrWhiteSpace (message)) {
				if (!shouldLogOut)
					ShowHelp (p);
				return;
			}

			await Task.WhenAll (UpdateTopic (message), SetMarkerInTwitch (message), TweetUpdate (message));
			Console.WriteLine ("Done!");


		}

		static void ShowHelp (OptionSet p)
		{
			Console.WriteLine ("Usage: greet [OPTIONS]+ message");
			Console.WriteLine ("Greet a list of individuals with an optional message.");
			Console.WriteLine ("If no message is specified, a generic greeting is used.");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			p.WriteOptionDescriptions (Console.Out);
		}

		static async Task<bool> SetMarkerInTwitch (string message)
		{
			var api = GetTwitchApi ();
			try {
				var response = await api.UpdateStreamMarker (message);
				Console.WriteLine (response);
				return true;
			} catch (Exception ex) {
				Console.WriteLine ("Failed to update Twitch");
				Console.WriteLine (ex);
				return false;
			}
		}

		static async Task<bool> UpdateTopic (string message)
		{
			try {
				var path = Settings.TextFilePath;
				if (string.IsNullOrWhiteSpace (path)) {
					Console.WriteLine ("Skipping writing to file, Filepath not set.");
					return false;
				}
				await File.WriteAllTextAsync (path, message);
				return true;
			} catch (Exception ex) {
				Console.WriteLine ("Failed to update Topic Text File");
				Console.WriteLine (ex);
				return false;
			}
		}

		static async Task<bool> TweetUpdate (string message)
		{
			try {
				var timeSinceTweet = DateTime.Now - Settings.LastTweetTime;
				if (timeSinceTweet < TimeSpan.FromMinutes (5)) {
					Console.WriteLine ($"Skipped Tweeting, too soon. Tweeted: {timeSinceTweet}");
					return false;
				}

				var api = GetTwitterApi ();

				var resp = await api.Post (null, "statuses/update.json", new Dictionary<string, string> {
					["status"] = $"{message} follow along at twitch.tv/Clancey"
				});
				Console.WriteLine ("Tweeting was Successful");
				Console.WriteLine (resp);
				return true;
			} catch (Exception ex) {
				Console.WriteLine ("Failed to update Twitter");
				Console.WriteLine (ex);
				return false;
			}
		}
		static HttpClient httpClient = new HttpClient ();
		public static async Task<string> UploadPhoto (string url)
		{
			const string path = "https://upload.twitter.com/1.1/media/upload.json";
			try {
				var imageResponse = await httpClient.GetAsync (url);
				var data = await imageResponse.Content.ReadAsByteArrayAsync ();
				var api = GetTwitterApi ();
				var encoded = Convert.ToBase64String (data);

				var parameters = new Dictionary<string, string> {
					["Name"] = url,
					["command"] = "INIT",
					["total_bytes"] = data.Length.ToString (),
					["media_type"] = "image/jpg",
				};
				var init = await api.Post<Dictionary<string, string>> (null, path,queryParameters:parameters,authenticated:true);
				var mediaId = init ["media_id"];

				parameters = new Dictionary<string, string> {
					["Name"] = url,
					["command"] = "APPEND",
					["media_id"] = mediaId,
					["segment_index"] = "0",
				};
				var multipartContent = new MultipartFormDataContent ();
				multipartContent.Add (new FormUrlEncodedContent (ToKeyValuePairs (parameters)));
				multipartContent.Add (new ByteArrayContent (data), "media_id");
				var result = await api.Post (new FormUrlEncodedContent (ToKeyValuePairs (parameters)), path);

				return mediaId;
				Console.WriteLine (result);
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
			return null;
		}

		static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs (Dictionary<string, string> dictionary) => dictionary.Select (x => new KeyValuePair<string, string> (x.Key, x.Value));



		static TwitterApi twitterApi;
		static TwitterApi GetTwitterApi () => twitterApi ?? (twitterApi = new TwitterApi (Settings.Profile, ApiConstants.TwitterApiKey, ApiConstants.TwitterSecret) {
			RedirectUrl = new Uri ("http://localhost"),
		});

		static TwitchApi twitchApi;
		static TwitchApi GetTwitchApi () => twitchApi ?? (twitchApi = new TwitchApi (Settings.Profile, ApiConstants.TwitchApiKey, ApiConstants.TwitchSecret) {
			Scopes = new [] {
					"clips:edit",
					"user:edit",
					"user:edit:broadcast",
				},
		});

	}
}
