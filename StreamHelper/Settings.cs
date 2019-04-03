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
			get => ToDateTime (CurrentSettings.GetValueOrDefault (nameof (LastTweetTime), ""));
			set => CurrentSettings.AddOrUpdateValue (nameof (LastTweetTime), ToString(value));
		}

		static DateTime ToDateTime(string s)
		{
			if (string.IsNullOrWhiteSpace (s))
				return DateTime.MinValue;
			var date = DateTime.Parse (s);
			return date;
		}

		static string ToString(DateTime date)
		{
			var s = date.ToString ();
			return s;
		}
	}
}
