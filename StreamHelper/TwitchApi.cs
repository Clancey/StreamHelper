using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SimpleAuth;
namespace StreamHelper
{
	public class TwitchApi : OAuthApi
	{
		public TwitchApi(string id, string clientId, string clientSecret) : base(id, clientId, clientSecret, "https://id.twitch.tv/oauth2/token", "https://id.twitch.tv/oauth2/authorize")
		{
			BaseAddress = new Uri("https://api.twitch.tv/helix/");
		}


		public async Task<TwitchUser> GetUserInfo(bool forceRefresh = false)
		{
			if (!HasAuthenticated)
				await Authenticate();
			string userInfoJson;
			if (forceRefresh || CurrentAccount == null || !CurrentAccount.UserData.TryGetValue("userInfo", out userInfoJson))
			{

				var response = await Get<TwitchUsersInfoResponse>("users");
				var user = response.Data.FirstOrDefault();
				userInfoJson = await user.ToJsonAsync();
				CurrentAccount.UserData["userInfo"] = userInfoJson;
				SaveAccount(CurrentAccount);
				return user;
			}

			return Deserialize<TwitchUser>(userInfoJson);

		}

		public async Task<string> UpdateStreamMarker(string description)
		{
			var user = await GetUserInfo();
			return await Post(new { user_id = user.Id, description = description }, "streams/markers");
		}
		public override Task PrepareClient(HttpClient client)
		{
			client.DefaultRequestHeaders.Add("Client-Id",this.ClientId);
			return base.PrepareClient(client);
		}

		public class TwitchUser
		{

			[JsonProperty("id")]
			public string Id { get; set; }

			[JsonProperty("login")]
			public string Login { get; set; }

			[JsonProperty("display_name")]
			public string DisplayName { get; set; }

			[JsonProperty("type")]
			public string Type { get; set; }

			[JsonProperty("broadcaster_type")]
			public string BroadcasterType { get; set; }

			[JsonProperty("description")]
			public string Description { get; set; }

			[JsonProperty("profile_image_url")]
			public string ProfileImageUrl { get; set; }

			[JsonProperty("offline_image_url")]
			public string OfflineImageUrl { get; set; }

			[JsonProperty("view_count")]
			public int ViewCount { get; set; }

			[JsonProperty("email")]
			public string Email { get; set; }
		}

		public class TwitchUsersInfoResponse
		{

			[JsonProperty("data")]
			public List<TwitchUser> Data { get; set; }
		}
	}
}
