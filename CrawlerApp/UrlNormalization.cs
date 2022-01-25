using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace CrawlerApp
{
    public static class UrlNormalization
    {
        public static bool AreTheSameUrls(this string url1, string url2)
        {
            url1 = url1.NormalizeUrl();
            url2 = url2.NormalizeUrl();
            return url1.Equals(url2);
        }

        public static bool AreTheSameUrls(this Uri uri1, Uri uri2)
        {
            var url1 = uri1.NormalizeUrl();
            var url2 = uri2.NormalizeUrl();
            return url1.Equals(url2);
        }

        private static string[] DefaultDirectoryIndexes = new[]
            {
                "default.asp",
                "default.aspx",
                "index.htm",
                "index.html",
                "index.php"
            };

        public static string NormalizeUrl(this Uri uri)
        {
            var url = UrlToLower(uri);
            url = LimitProtocols(url);
            url = RemoveDefaultDirectoryIndexes(url);
            url = RemoveTheFragment(url);
            url = RemoveDuplicateSlashes(url);
            url = AddWww(url);
            url = RemoveFeedburnerPart(url);
            return RemoveTrailingSlashAndEmptyQuery(url);
        }

        public static string NormalizeUrl(this string url)
        {
            return NormalizeUrl(new Uri(url));
        }

        public static bool IsInsideTheMainDomain(this Uri currentUri, Uri domainUri)
        {
            string sDomainUrl = domainUri.Host + domainUri.AbsolutePath;
            string currentUrl = currentUri.Host + currentUri.Segments.FirstOrDefault();
            return sDomainUrl.Contains(currentUrl);
        }

        public static bool IsValidUrl(this string url)
        {
            Regex regexLink = new Regex(@"(http|ftp|https):\/\/([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])");
            return regexLink.IsMatch(url);
        }

        private static string RemoveFeedburnerPart(string url)
        {
            var idx = url.IndexOf("utm_source=", StringComparison.Ordinal);
            return idx == -1 ? url : url.Substring(0, idx - 1);
        }

        private static string AddWww(string url)
        {
            if (new Uri(url).Host.Split('.').Length == 2 && !url.Contains("://www."))
            {
                return url.Replace("://", "://www.");
            }
            return url;
        }

        private static string RemoveDuplicateSlashes(string url)
        {
            var path = new Uri(url).AbsolutePath;
            return path.Contains("//") ? url.Replace(path, path.Replace("//", "/")) : url;
        }

        private static string LimitProtocols(string url)
        {
            return new Uri(url).Scheme == "https" ? url.Replace("https://", "http://") : url;
        }

        private static string RemoveTheFragment(string url)
        {
            var fragment = new Uri(url).Fragment;
            return string.IsNullOrWhiteSpace(fragment) ? url : url.Replace(fragment, string.Empty);
        }

        private static string UrlToLower(Uri uri)
        {
            return HttpUtility.UrlDecode(uri.AbsoluteUri.ToLowerInvariant());
        }

        private static string RemoveTrailingSlashAndEmptyQuery(string url)
        {
            return url
                    .TrimEnd(new[] { '?' })
                    .TrimEnd(new[] { '/' });
        }

        private static string RemoveDefaultDirectoryIndexes(string url)
        {
            foreach (var index in DefaultDirectoryIndexes)
            {
                if (url.EndsWith(index))
                {
                    url = url.TrimEnd(index.ToCharArray());
                    break;
                }
            }
            return url;
        }
    }
}
