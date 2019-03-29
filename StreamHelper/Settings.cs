using System;
using Plugin.Settings.Abstractions;

namespace StreamHelper {
	public static class Settings {
		static ISettings CurrentSettings => Plugin.Settings.CrossSettings.Current;

		public static string TextFilePath {
			get => CurrentSettings.GetValueOrDefault (nameof (TextFilePath), "");
			set => CurrentSettings.AddOrUpdateValue (nameof (TextFilePath), value);
		}


		public static string Profile {
			get => CurrentSettings.GetValueOrDefault (nameof (Profile), "Default");
			set => CurrentSettings.AddOrUpdateValue (nameof (Profile), value);
		}


		public static DateTime LastTweetTime {
			get => CurrentSettings.GetValueOrDefault (nameof (LastTweetTime), DateTime.MinValue);
			set => CurrentSettings.AddOrUpdateValue (nameof (LastTweetTime), value);
		}
}
