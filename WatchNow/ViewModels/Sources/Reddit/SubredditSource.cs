using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WatchNow.Helpers;

namespace WatchNow.Avalonia.ViewModels.Sources
{
    public class SubredditSource : SourceViewModel
    {
        private string subreddit;

        public SubredditSource(string subreddit)
        {
            Header = subreddit;
            this.subreddit = subreddit;
        }

        private static readonly HttpClient _http = new HttpClient()
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:142.0) Gecko/20100101 Firefox/142.0" }
            }
        };

        private class RedditPost
        {
            public string url { get; set; }
            public string thumbnail { get; set; }
            public bool stickied { get; set; }
            public string title { get; set; }
            public string id { get; set; }
        }


        public override async Task LoadItems(int amount, Action<int> addVideoCount, Action incrementProgressBar, Action<string> loadVideoAction)
        {
            List<RedditPost> posts = await GetPostsForSubreddit(subreddit, amount + 5 /* Get 5 more than the requested amount so we have room for "stickied posts"*/);
            posts.RemoveAll(p => p.stickied);

            int finalAmount = Math.Min(amount, posts.Count);

            addVideoCount?.Invoke(finalAmount);

            foreach (RedditPost post in posts.Take(finalAmount))
            {
                string videoUrl = post.url.ToString();
                YouTubeHelper.TryParseYouTubeVideoId(videoUrl, out string videoId);

                Match timestamp = Regex.Match(videoUrl, @"t=(?<hour>\d+h)?(?<minute>\d+m)?(?<second>\d+s)|t=(?<other>[^&]+)");
                videoUrl = $"https://www.youtube.com/embed/{videoId}";

                if (timestamp.Success)
                {
                    int timestampSeconds = 0;
                    if (!string.IsNullOrEmpty(timestamp.Groups["hour"].Value))
                    {
                        timestampSeconds += int.Parse(timestamp.Groups["hour"].Value.Replace("h", "")) * 60 * 60;
                    }

                    if (!string.IsNullOrEmpty(timestamp.Groups["minute"].Value))
                    {
                        timestampSeconds += int.Parse(timestamp.Groups["minute"].Value.Replace("m", "")) * 60;
                    }

                    if (!string.IsNullOrEmpty(timestamp.Groups["second"].Value))
                    {
                        timestampSeconds += int.Parse(timestamp.Groups["second"].Value.Replace("s", ""));
                    }

                    if (timestampSeconds > 0)
                    {
                        videoUrl += $"?start={timestampSeconds}";
                    }
                    else if (!string.IsNullOrEmpty(timestamp.Groups["other"].Value))
                    {
                        videoUrl += $"?start={timestamp.Groups["other"].Value}";
                    }
                }

                string imageUrl = post.thumbnail.ToString() == "spoiler" || post.thumbnail.ToString() == "nsfw" || post.thumbnail.ToString() == "default" ? null : post.thumbnail.ToString();
                Items.Add(new VideoListingViewModel
                {
                    Text = $"{System.Net.WebUtility.HtmlDecode(post.title)}\n\n/r/videos", //HtmlDecode used to change "&amp; to &", etc
                    ImageUrl = System.Net.WebUtility.HtmlDecode(imageUrl), // Todo: unfortunately, this often doesn't load because reddit prevents some requests that don't provide a user agent string
                    LoadVideoAction = () => loadVideoAction(videoUrl),
                    OpenInBrowserAction = () => Common.OpenUrl(videoUrl),
                    ActionButtonVisible = true,
                    ActionButtonText = "Comments",
                    ActionButtonAction = () => Common.OpenUrl($"https://reddit.com/comments/{post.id}"),
                });

                incrementProgressBar();
            }
        }

        private async Task<List<RedditPost>> GetPostsForSubreddit(string subreddit, int amount)
        {
            string response = await _http.GetStringAsync($"https://www.reddit.com{subreddit}.json?limit={amount}");
            JObject jObject = JObject.Parse(response);

            List<RedditPost> posts = new List<RedditPost>();
            foreach (var child in jObject["data"]["children"])
            {
                var post = child["data"].ToObject<RedditPost>();
                posts.Add(post);
            }

            return posts;
        }
    }
}
