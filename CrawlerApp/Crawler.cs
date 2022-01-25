using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CrawlerApp
{
    public class Crawler
    {
        private readonly ISet<string> _urlListCache;
        private readonly string _mainDomainUrl;
        private readonly Uri _mainDomainUri;
        private readonly string _mode;
        private readonly string _startCommand;

        public Crawler(string[] args)
        {
            this._startCommand = args[0];
            this._mode = args[1];
            this._mainDomainUrl = args[2].NormalizeUrl();
            this._mainDomainUri = new Uri(this._mainDomainUrl);
            this._urlListCache = new HashSet<string>();
        }

        public async Task Execute()
        {
            if (!this._startCommand.Equals("wcraw"))
            {
                Console.WriteLine("The command to start the program is not valid!");
                return;
            }
            if (!this._mainDomainUrl.IsValidUrl())
            {
                Console.WriteLine("The url is not valid!");
                return;
            }
            if (!(this._mode.Equals("-r") || this._mode.Equals("-n")))
            {
                Console.WriteLine("The entered mode is not valid");
                return;
            }

            await this.ExecuteHttpRequest(this._mainDomainUrl);
        }

        private async Task ExecuteHttpRequest(string url)
        {
            var client = new RestClient();
            var request = new RestRequest(url)
            {
                Timeout = 6000,
                Method = Method.Get
            };
            var cancellationTokenSource = new CancellationTokenSource();
            var response = await client.ExecuteAsync(request, cancellationTokenSource.Token);
            this.PrintResponse(response);
            await this.ProcessResponse(response);
        }

        private void PrintResponse(RestResponse response)
        {
            Console.WriteLine();
            Console.WriteLine("Resolving... " + (response.Request.Resource));
            Console.WriteLine("Response... " + ((int)response.StatusCode).ToString() + " " + (response.StatusDescription));
            Console.WriteLine();
        }

        private async Task ProcessResponse(RestResponse response)
        {
            var urlList = this.GetNewUrlListFromHtmlContent(response.Content);

            foreach (var url in urlList)
            {
                if (this._urlListCache.Contains(url) is false)
                {
                    this._urlListCache.Add(url.ToString());

                    if (this._mode.Equals("-r"))
                        await this.ExecuteHttpRequest(url);
                }
            }
        }

        private ISet<string> GetNewUrlListFromHtmlContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new HashSet<string>();

            ISet<string> newUrlList = new HashSet<string>();
            MatchCollection matches = GetUrlMatchesFromHtmlContent(content);

            foreach (var match in matches)
            {
                var matchUri = new Uri(match.ToString());

                if (matchUri.IsInsideTheMainDomain(this._mainDomainUri))
                    this.AddUrlSegmentsToList(newUrlList, matchUri);
            }
            return newUrlList;
        }

        private static MatchCollection GetUrlMatchesFromHtmlContent(string content)
        {
            Regex regexLink = new Regex(@"(http|ftp|https):\/\/([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-])");
            var matches = regexLink.Matches(content);
            return matches;
        }

        private void AddUrlSegmentsToList(ISet<string> urlList, Uri matchUri)
        {
            var segmentUrl = new StringBuilder();
            segmentUrl.Append(matchUri.Scheme + "://")
                      .Append(matchUri.Host);

            foreach (var segment in matchUri.Segments)
            {
                segmentUrl.Append(segment);
                var normalizedUrl = segmentUrl.ToString().NormalizeUrl();
                if (urlList.Contains(normalizedUrl) is false)
                    urlList.Add(normalizedUrl);
            }
        }
    }
}
