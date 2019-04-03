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
using System.Net.Http.Headers;

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
			Console.WriteLine ("Usage: StreamHelper [OPTIONS]+ message");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			p.WriteOptionDescriptions (Console.Out);
		}

		static async Task<bool> SetMarkerInTwitch (string message)
		{
			var api = GetTwitchApi ();
			try {
				var response = await api.UpdateStreamMarker (message);
				Console.WriteLine ("Twitch updating was Successfull");
				Debug.WriteLine (response);
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

				//TODO: Configure upload image turn off
				var imageUrl = await GetTwitchLiveFeedImageUrl ();
				var mediaId = await UploadPhoto (imageUrl);

				var api = GetTwitterApi ();
				var twitchUrl = await GetTwitchStreamUrl ();
				var tweetData = new Dictionary<string, string> {
					["status"] = $"{message} follow along at {twitchUrl}",
				};
				if (!string.IsNullOrWhiteSpace (mediaId))
					tweetData ["media_ids"] = mediaId;

				var resp = await api.Post (null, "statuses/update.json", tweetData);
				Console.WriteLine ("Tweeting was Successful");
				Debug.WriteLine (resp);
				Settings.LastTweetTime = DateTime.Now;
				return true;
			} catch (Exception ex) {
				Console.WriteLine ("Failed to update Twitter");
				Console.WriteLine (ex);
				return false;
			}
		}

		static async Task<string> GetTwitchLiveFeedImageUrl()
		{
			var api = GetTwitchApi ();
			var user = await api.GetUserInfo ();
			return $"https://static-cdn.jtvnw.net/previews-ttv/live_user_{user.Login}-1280x720.jpg";
		}


		static async Task<string> GetTwitchStreamUrl ()
		{
			var api = GetTwitchApi ();
			var user = await api.GetUserInfo ();
			return $"twitch.tv/{user.Login}";
		}


		static HttpClient httpClient = new HttpClient ();
		public static async Task<string> UploadPhoto (string url)
		{
			const string path = "https://upload.twitter.com/1.1/media/upload.json";
			try {
				//Download the image
				var imageResponse = await httpClient.GetAsync (url);
				var data = await imageResponse.Content.ReadAsByteArrayAsync ();
				var api = GetTwitterApi ();
				//Upload it to twitter!
				var imageContent = new ByteArrayContent (data);
				imageContent.Headers.ContentType = new MediaTypeHeaderValue ("multipart/form-data");
				var multipartContent = new MultipartFormDataContent {
					{ imageContent, "media" }
				};
				var result = await api.Post<Dictionary<string, string>> (multipartContent, path);
				var mediaId = result ["media_id"];

			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
			return null;
		}

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
