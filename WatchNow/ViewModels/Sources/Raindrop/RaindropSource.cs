using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using WatchNow.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WatchNow.Avalonia.ViewModels.Sources
{
    public class RaindropSource : SourceViewModel
    {
		private string authToken;

        public RaindropSource()
        {
            Header = "Raindrop";
        }

        public override void Auth()
        {
            authToken = Common.ReadSetting("RaindropAuthToken", "");
            if (string.IsNullOrEmpty(authToken))
			{
				Common.ShowMessageBox("Raindrop auth token missing", "'RaindropAuthToken' is not set in Settings.ini. Please go to https://app.raindrop.io/settings/integrations,\ncreate a new app, create a test token, and put the token in Settings.ini as 'RaindropAuthToken=TOKEN'");
			}

            //Todo: this should use oauth to authenticate, not the "TestToken" https://developer.raindrop.io/v1/authentication/token
            // We will then also have to handle refreshing the token: https://developer.raindrop.io/v1/authentication/token#the-access-token-refresh
        }

		private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://api.raindrop.io/rest/v1/";

		private class RainDropBookmark // I've only implemented the things I need. Here's the full structure: https://developer.raindrop.io/v1/collections/methods
		{
			[JsonProperty("_id")]
			public int Id { get; set; }
			public DateTime Created { get; set; }
			public string Title { get; set; }
			public string Link { get; set; }
			public string Cover { get; set; }
			public string Type { get; set; }
		}

        public override async Task LoadItems(int amount, Action<int> addVideoCount, Action incrementProgressBar, Action<string> loadVideoAction)
        {
			try
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

				var response = await client.GetAsync($"{BaseUrl}raindrops/0"); // 0 = all bookmarks
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync();
				var json = JObject.Parse(content);

				foreach (RainDropBookmark bookmark in json["items"].ToObject<List<RainDropBookmark>>())
				{
					if (bookmark.Type != "video")
					{
						continue;
					}

					YouTubeHelper.TryParseYouTubeVideoId(bookmark.Link, out string youtubeVideoId);

					VideoListingViewModel videoItem = new VideoListingViewModel
					{
						Text = bookmark.Title,
						ImageUrl = bookmark.Cover,
						LoadVideoAction = () => loadVideoAction(!string.IsNullOrEmpty(youtubeVideoId) ? $"https://youtube.com/embed/{youtubeVideoId}" : bookmark.Link),
						OpenInBrowserAction = () => Common.OpenUrl(!string.IsNullOrEmpty(youtubeVideoId) ? $"https://youtube.com/embed/{youtubeVideoId}" : bookmark.Link),
						ActionButtonVisible = true,
						ActionButtonText = "Archive",
						NewLabelVisible = Common.NewLabelVisible(bookmark.Created),
					};

					videoItem.ActionButtonAction = () =>
					{
						_ = DeleteBookmark(bookmark.Id);
						Items.Remove(videoItem);
					};

					Items.Add(videoItem);

					incrementProgressBar();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error fetching bookmarks: {ex.Message}");
			}
		}

		private async Task DeleteBookmark(int raindropId)
		{
			try
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

				var response = await client.DeleteAsync($"{BaseUrl}raindrop/{raindropId}");

				if (response.IsSuccessStatusCode)
				{
					Console.WriteLine($"Successfully deleted bookmark with ID {raindropId}.");
				}
				else
				{
					Console.WriteLine($"Failed to delete bookmark. Status code: {response.StatusCode}");
					string errorDetails = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"Details: {errorDetails}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error deleting bookmark: {ex.Message}");
			}
		}
    }
}
