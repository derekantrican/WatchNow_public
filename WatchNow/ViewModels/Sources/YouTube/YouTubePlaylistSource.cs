using System;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using WatchNow.Helpers;

namespace WatchNow.Avalonia.ViewModels.Sources
{
    public class YouTubePlaylistSource : SourceViewModel
    {
        public string PlaylistId { get; set; }
        public SyndicationFeed PlaylistInfo { get; set; }

        public YouTubePlaylistSource(string playlistId)
        {
            PlaylistId = playlistId;
        }

        public override async Task LoadItems(int amountToList /*Due to a limitation with YouTube RSS feeds, this will only return the top 15 items on the playlist*/, Action<int> addVideoCount, Action incrementProgressBar, Action<string> loadVideoAction)
        {
            PlaylistInfo = await GetPlaylistInfoAsync(PlaylistId);
            Header = PlaylistId.StartsWith("UU") ? PlaylistInfo.Authors[0].Name : PlaylistInfo.Title.Text;

            addVideoCount?.Invoke(PlaylistInfo.Items.Count());
            
            foreach (SyndicationItem syndicationItem in PlaylistInfo.Items)
            {
                try
                {
                    string videoId = syndicationItem.GetExtensionValue(["videoId"]);
                    string videoTitle = syndicationItem.GetExtensionValue(["group", "title"]);
                    string videoThumbnail = syndicationItem.GetExtensionValue(["group", "thumbnail"], "url");
                    DateTime publishedAt = syndicationItem.PublishDate.LocalDateTime;

                    Items.Add(new VideoListingViewModel
                    {
                        Text = $"{videoTitle}\n\n{Header}",
                        ImageUrl = videoThumbnail,
                        LoadVideoAction = () => loadVideoAction($"https://youtube.com/embed/{videoId}"),
                        OpenInBrowserAction = () => Common.OpenUrl($"https://youtube.com/embed/{videoId}"),
                        NewLabelVisible = Common.NewLabelVisible(publishedAt),
                    });

                    incrementProgressBar();
                }
                catch (Exception ex)
                {
                }
			}
		}

        private static readonly HttpClient _http = new HttpClient();

        private static async Task<SyndicationFeed> GetPlaylistInfoAsync(string playlistId)
		{
            var url = $"https://www.youtube.com/feeds/videos.xml?playlist_id={playlistId}";

            await using var stream = await _http.GetStreamAsync(url).ConfigureAwait(false);
            using (XmlReader reader = XmlReader.Create(stream))
			{
				return SyndicationFeed.Load(reader);
			}
		}
	}
}
