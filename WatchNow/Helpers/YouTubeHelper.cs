using System.Text.RegularExpressions;

namespace WatchNow.Helpers
{
    public static class YouTubeHelper
    {
        public static bool TryParseYouTubeVideoId(string videoUrl, out string videoId)
        {
            videoId = default;

            if (string.IsNullOrWhiteSpace(videoUrl))
                return false;

            // https://www.youtube.com/watch?v=yIVRs6YSbOM
            var regularMatch = Regex.Match(videoUrl, @"youtube\..+?/watch.*?v=(.*?)(?:&|/|$)").Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(regularMatch) && ValidateVideoId(regularMatch))
            {
                videoId = regularMatch;
                return true;
            }

            // https://youtu.be/yIVRs6YSbOM
            var shortMatch = Regex.Match(videoUrl, @"youtu\.be/(.*?)(?:\?|&|/|$)").Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(shortMatch) && ValidateVideoId(shortMatch))
            {
                videoId = shortMatch;
                return true;
            }

            // https://www.youtube.com/embed/yIVRs6YSbOM
            var embedMatch = Regex.Match(videoUrl, @"youtube\..+?/embed/(.*?)(?:\?|&|/|$)").Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(embedMatch) && ValidateVideoId(embedMatch))
            {
                videoId = embedMatch;
                return true;
            }

            return false;
        }

        private static bool ValidateVideoId(string videoId)
        {
            if (string.IsNullOrWhiteSpace(videoId))
                return false;

            // Video IDs are always 11 characters
            if (videoId.Length != 11)
                return false;

            return !Regex.IsMatch(videoId, @"[^0-9a-zA-Z_\-]");
        }
    }
}
