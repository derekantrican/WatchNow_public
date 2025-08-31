using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace WatchNow.Helpers
{
    public static class Common
    {
        public static string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WatchNow");
        public static string SettingsFile = Path.Combine(SettingsFolder, "Settings.ini");
        public static string CEFCacheFolder = Path.Combine(SettingsFolder, "CEFCache");

		public static Action<string, string> ShowMessageBox;

        public static bool NewLabelVisible(DateTime uploadDate)
        {
            double diff = Math.Round((DateTime.Now - uploadDate).TotalDays, 2);
            if (uploadDate.DayOfWeek == DayOfWeek.Monday ||
                uploadDate.DayOfWeek == DayOfWeek.Sunday ||
                uploadDate.DayOfWeek == DayOfWeek.Saturday)
            {
                return diff <= 3;
            }
            else
            {
                return diff <= 1;
            }
        }

		public static JArray GetSponsorBlockSegments(string youtubeVideoId)
		{
			string url = $"https://sponsor.ajay.app/api/skipSegments?videoID={youtubeVideoId}&categories=[\"sponsor\",\"intro\",\"outro\",\"selfpromo\",\"interaction\"]";

			using (HttpClient client = new HttpClient())
			{
				HttpResponseMessage response = client.Send(new HttpRequestMessage(HttpMethod.Get, url));
				return JArray.Parse(response.Content.ReadAsStringAsync().Result);
			}
		}

		public static void OpenUrl(string url)
		{
			Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true }); // hack because of this: https://github.com/dotnet/corefx/issues/10361
		}

		//Todo: if we improve this in the future, we should probably do this in a better way. This was just the quickest thing I could think of
		#region Settings
		public static T ReadSetting<T>(string settingName, T defaultValue = default)
		{
			if (!File.Exists(SettingsFile))
				return defaultValue;

			foreach (string setting in File.ReadAllLines(SettingsFile))
			{
				if (setting.StartsWith(settingName + "="))
					return (T)Convert.ChangeType(setting.Split(new[] { '=' }, 2)[1], typeof(T));
			}

			return defaultValue;
		}

		private static object settingsLocker = new object();
		public static void SaveSetting(string settingName, object value)
		{
			if (!File.Exists(SettingsFile))
			{
				if (!Directory.Exists(SettingsFolder))
				{
					Directory.CreateDirectory(SettingsFolder);
				}

                File.Create(SettingsFile).Close();
			}

			List<string> settings = File.ReadAllLines(SettingsFile).ToList();
			int index = settings.FindIndex(p => p.StartsWith(settingName + "="));
			if (index > -1)
			{
				settings.RemoveAt(index);
			}

			settings.Add(settingName + "=" + value.ToString());

			lock (settingsLocker)
			{
				File.WriteAllLines(SettingsFile, settings.ToArray());
			}
		}
		#endregion Settings
	}
}
