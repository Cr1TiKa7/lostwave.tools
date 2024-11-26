using HtmlAgilityPack;
using Lostwave.Tools.Models;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Lostwave.Tools.Services
{
    public class MySpaceLogEventArgs
    {
        public string Message { get; set; }
        public string Username { get; set; }
        public string SearchTerm { get; set; }
        public string Guid { get; set; }
    }

    public class MySpaceCrawlingService
    {
        public EventHandler<MySpaceLogEventArgs> Log;
        #region "RegEx"

        private Regex _baseUrlRegex = new Regex(@"curl \'(?<url>.*)\'.*\\");
        private Regex _headerRegex = new Regex(@"-H \'(?<headerName>.*)\: (?<headerValue>.*)\' \\");
        private Regex _songContentRegex = new Regex(@"--data-raw \'page=(?<page>[0-9]{1,}).*&ssid=(?<ssid>.*)\'");
        private Regex _connectionsContentRegex = new Regex(@"--data-raw \'start=(?<start>[0-9]{1,}).*'");
        #endregion
        /// <summary>
        /// This will be written into the header of the webrequest to authorize our request. Idk yet how long this one is valid
        /// </summary>
        private const string _crawlingHashHeaderValue = "MDAwNWQyZTU1Y2UwYmQ0NMOZaiHDrcOeVDLDg3jCsHJzRcKcfTkmwpTDgX9kDMO1w4rDjnPCuAvDrg7CnWLChDwDTMOUEWFLaTNuWwxmwq5vw6LDtjp/TMK+w7NLw4XDgyZKw7wUwqoMRcOlwrrDpMKgw4sfPFvDnBrDrgcqBsO2bsO7YMKdcMKgw60Ew5rDn13CpcOIQ8OneMOJcVbClSjDojpjfVlRB8OSU8KkYw%3D%3D";

        #region  "Song crawl stuff"

        private string _songCrawlingCurl = @"curl 'https://myspace.com/ajax/page/search/songs?q={searchTerm}' \
  -H 'accept: */*' \
  -H 'accept-language: de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7' \
  -H 'cache-control: no-cache' \
  -H 'client: persistentId=88092f65-0394-4c31-b351-cb9737026797&screenWidth=1920&screenHeight=1080&timeZoneOffsetHours=-2&visitId=c0fcdb10-6319-4fd1-a123-c36754695bf7&windowWidth=605&windowHeight=883' \
  -H 'content-type: application/x-www-form-urlencoded; charset=UTF-8' \
  -H 'cookie: playerControlTip=shown=true; persistent_id=pid%3D88092f65-0394-4c31-b351-cb9737026797%26llid%3D-1%26lprid%3D-1%26lltime%3D2024-07-01T13%253A49%253A52.210Z; geo=false; visit_id=c0fcdb10-6319-4fd1-a123-c36754695bf7; beacons_enabled=true; OptanonConsent=isGpcEnabled=0&datestamp=Tue+Jul+09+2024+00%3A30%3A45+GMT%2B0200+(Mitteleurop%C3%A4ische+Sommerzeit)&version=202211.2.0&isIABGlobal=false&hosts=&landingPath=NotLandingPage&groups=C0001%3A1%2CC0003%3A1%2CSSPD_BG%3A0%2CC0002%3A0%2CC0004%3A0&AwaitingReconsent=false; player=sequenceId=1&paused=true&currentTime=0&volume=0.5&mute=false&shuffled=false&repeat=off&mode=queue&pinned=false&streamStartDateTime=2024-07-04T11%3A09%3A20.914Z&at=360&incognito=false&allowSkips=true&ccOn=false' \
  -H 'hash: MDAwNWQyZTU1Y2UwYmQ0NMOZaiHDrcOeVDLDg3jCsHJzRcKcfTkmwpTDgX9kDMO1w4rDjnPCuAvDrg7CnWLChDwDTMOUEWFLaTNuWwxmwq5vw6LDtjp/TMK+w7NLw4XDgyZKw7wUwqoMRcOlwrrDpMKgw4sfPFvDnBrDrgcqBsO2bsO7YMKdcMKgw60Ew5rDn13CpcOIQ8OneMOJcVbClSjDojpjfVlRB8OSU8KkYw%3D%3D' \
  -H 'origin: https://myspace.com' \
  -H 'pragma: no-cache' \
  -H 'priority: u=1, i' \
  -H 'referer: https://myspace.com/search/songs?q={searchTerm}' \
  -H 'sec-ch-ua: ""Not/A)Brand"";v=""8"", ""Chromium"";v=""126"", ""Google Chrome"";v=""126""' \
  -H 'sec-ch-ua-mobile: ?0' \
  -H 'sec-ch-ua-platform: ""Windows""' \
  -H 'sec-fetch-dest: empty' \
  -H 'sec-fetch-mode: cors' \
  -H 'sec-fetch-site: same-origin' \
  -H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36' \
  -H 'x-requested-with: XMLHttpRequest' \
  --data-raw 'page=2&ssid=c4f138f8-f84a-4b5a-bba1-8a5f952d4085'";
        public async Task<int> GetSongCountForSearchTerm(string searchTerm)
        {
            while (true)
            {
                var web = new HtmlWeb();
                var doc = await web.LoadFromWebAsync($"https://myspace.com/search/songs?q={searchTerm}");

                var songCount = doc.DocumentNode.SelectSingleNode("//div[@id='resultsBlock']/h3")?.Attributes["data-total"]?.Value;

                if (songCount == null)
                {
                    await Task.Delay(1000);
                    continue;
                }
                if (int.TryParse(songCount, out var cnt))
                    return cnt;
                else
                    return -1;
            };
        }
        private int _songCount;
        public async Task<List<SongItem>> CrawlSongsByTerm(string term, string guid)
        {
            term = HttpUtility.HtmlEncode(term);
            _songCount = await GetSongCountForSearchTerm(term);
            var tmp = _songCrawlingCurl.Replace("{searchTerm}", term);
            return await CrawlSongs(tmp, term, guid);
        }
        private int _maxPageTries = 2;
        public async Task<List<SongItem>> CrawlSongs(string curl, string term, string guid, int tries = 10)
        {
            var ret = new List<SongItem>();
            var page = 1;
            while (true)
            {
                try
                {
                    var res = await CrawlPage(curl, term, guid, page, tries);

                    if (res?.Count > 0)
                    {
                        _maxPageTries = 2;
                        ret.AddRange(res);

                        page = page + 1;
                        if (page > (_songCount / 20) + 1)
                            break;
                    }
                    else
                    {
                        if (_maxPageTries == 0)
                            break;
                        else
                        {
                            page = page + 1;
                            tries = 10;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex = ex;
                    await Task.Delay(1000);
                }
            }
            return ret;
        }
        private async Task<List<SongItem>> CrawlPage(string text, string term, string guid, int page = 1, int tries = 10)
        {

            while (tries > 0)
            {
                try
                {

                    Log?.Invoke(this, new MySpaceLogEventArgs
                    {
                        Message = $"Crawling page '{page}/{(int)(_songCount / 20) + 1}' | Tries left: {tries}",
                        Username = term,
                        SearchTerm = term,
                        Guid = guid
                    });
                    var ret = new List<SongItem>();
                    var wr = ParseSongCurl(text, page);
                    var res = await wr.GetResponseAsync();
                    using (var responseStream = res.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        var tmp = reader.ReadToEnd();

                        if (!string.IsNullOrEmpty(tmp) && tmp != "{}")
                        {
                            var doc = new HtmlDocument();
                            doc.LoadHtml(tmp);

                            foreach (var listNode in doc.DocumentNode.SelectNodes("//li/div[@class='flex']"))
                            {
                                var titleNode = listNode.SelectSingleNode("./div[@class='title']");
                                var artistNode = listNode.SelectSingleNode("./div[@class='artist']");
                                var albumNode = listNode.SelectSingleNode("./div[@class='album']");
                                var dateNode = listNode.SelectSingleNode("./div[@class='date']");
                                var durationNode = listNode.SelectSingleNode("./div[@class='duration']");


                                ret.Add(new SongItem
                                {
                                    Album = albumNode?.InnerText.Trim().Replace("\n", ""),
                                    Artist = artistNode?.InnerText.Trim().Replace("\n", ""),
                                    Date = dateNode?.InnerText.Trim().Replace("\n", ""),
                                    Duration = durationNode?.InnerText.Trim().Replace("\n", ""),
                                    Url = "https://myspace.com" + titleNode?.SelectSingleNode("./a")?.Attributes["href"]?.Value,
                                    Title = titleNode?.InnerText.Trim()
                                });

                            }
                            return ret;
                        }
                        else
                        {
                            tries = tries - 1;
                            await Task.Delay(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    //Log?.Invoke(this, new MySpaceLogEventArgs
                    //{
                    //    Message = $"Error: {ex}",
                    //    Username = term,
                    //     SearchTerm = term,
                    //     Guid = guid
                    //});
                }
            }
            _maxPageTries = _maxPageTries - 1;
            return null;
        }
        private HttpWebRequest ParseSongCurl(string curl, int page = 1)
        {
            var baseUrl = _baseUrlRegex.Match(curl).Groups["url"].Value;

            var webRequest = (HttpWebRequest)WebRequest.Create(baseUrl);
            webRequest.Method = "POST";
            foreach (Match headeRegexResult in _headerRegex.Matches(curl))
            {
                if (headeRegexResult.Success)
                {
                    var name = headeRegexResult.Groups["headerName"].Value;
                    var value = headeRegexResult.Groups["headerValue"].Value;

                    switch (name)
                    {
                        case "accept":
                            webRequest.Accept = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "content-type":
                            webRequest.ContentType = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "referer":
                            webRequest.Referer = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "user-agent":
                            webRequest.UserAgent = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "hash":
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                        default:
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                    }

                }
            }

            var ssid = _songContentRegex.Match(curl).Groups["ssid"].Value;
            using (StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                requestWriter.Write($"page={page}&ssid={ssid}");
            }



            return webRequest;
        }

        #endregion

        #region "Connections crawl stuff

        private const string _connectionsCrawlingCulr = @"curl 'https://myspace.com/ajax/{user}/connections/out' \
              -H 'accept: */*' \
              -H 'accept-language: de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7' \
              -H 'cache-control: no-cache' \
              -H 'client: persistentId=88092f65-0394-4c31-b351-cb9737026797&screenWidth=1920&screenHeight=1080&timeZoneOffsetHours=-2&visitId=7ccfc77d-5d51-42a3-a98b-7939133e804b&windowWidth=1349&windowHeight=911' \
              -H 'content-type: application/x-www-form-urlencoded; charset=UTF-8' \
              -H 'cookie: playerControlTip=shown=true; persistent_id=pid%3D88092f65-0394-4c31-b351-cb9737026797%26llid%3D-1%26lprid%3D-1%26lltime%3D2024-07-01T13%253A49%253A52.210Z; geo=false; OptanonAlertBoxClosed=2024-07-10T16:56:10.505Z; visit_id=7ccfc77d-5d51-42a3-a98b-7939133e804b; beacons_enabled=true; player=sequenceId=3&paused=true&currentTime=0&volume=0.2170138888888889&mute=false&shuffled=false&repeat=off&mode=queue&pinned=false&at=360&incognito=false&allowSkips=true&ccOn=false; OptanonConsent=isGpcEnabled=0&datestamp=Sat+Jul+13+2024+19%3A00%3A51+GMT%2B0200+(Mitteleurop%C3%A4ische+Sommerzeit)&version=202406.1.0&isIABGlobal=false&hosts=&landingPath=NotLandingPage&groups=C0003%3A1%2CC0001%3A1%2CC0002%3A1%2CC0004%3A1%2CSSPD_BG%3A1&AwaitingReconsent=false&browserGpcFlag=0&geolocation=DE%3BNW' \
              -H 'hash: YzI1ZGU5MDRjOTY2NDc5ZWDCscOCcywHwqQ5wpLDvnhKw7rCowZ8wqfCs0oiOsK4NC58w6YjfcKFwowGw6LCj8OoDMOgwr/CksOHWiMDw5bDrMKMwrs5SDgAfMKhC0DDsyg2w7rDqkUYEsKswro8wonDgH7DiWRrw5V4W8KLwrjDksKRCnEDSkVXwrtJw4EnPsKTwo9bLsOxURcLIBLDkXBaw50pw61gZUJvwr/Cu8KU' \
              -H 'origin: https://myspace.com' \
              -H 'pragma: no-cache' \
              -H 'previousreferrer: https://myspace.com/' \
              -H 'priority: u=1, i' \
              -H 'referer: https://myspace.com/{user}/connections/out' \
              -H 'sec-ch-ua: ""Not/A)Brand"";v=""8"", ""Chromium"";v=""126"", ""Google Chrome"";v=""126""' \
              -H 'sec-ch-ua-mobile: ?0' \
              -H 'sec-ch-ua-platform: ""Windows""' \
              -H 'sec-fetch-dest: empty' \
              -H 'sec-fetch-mode: cors' \
              -H 'sec-fetch-site: same-origin' \
              -H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36' \
              -H 'x-requested-with: XMLHttpRequest' \
              --data-raw 'start=30'";



        private int GetConnectionCountOfUser(string username)
        {
            while (true)
            {
                var web = new HtmlWeb();
                var doc = web.LoadFromWebAsync($"https://myspace.com/{username}/connections/out").Result;

                var songCount = doc.DocumentNode.SelectSingleNode("//section[@id='sidebar']/footer/div/h5").InnerText.Replace(" Connections", "");

                if (songCount == null)
                {
                    Task.Delay(1000);
                    continue;
                }
                if (int.TryParse(songCount.Replace(",", ""), out var cnt))
                    return cnt;
                else
                    return -1;
            };
        }

        public async Task<List<UserConnectionItem>> GetUserConnection(string user, string guid, int tries = 5)
        {
            var maxConnectionCount = 0;
            try
            {
                maxConnectionCount = GetConnectionCountOfUser(user);
            }
            catch (Exception ex)
            {
                maxConnectionCount = -1;
            }
            var ret = new List<UserConnectionItem>();
            var page = 0;
            while (tries > 0)
            {
                try
                {
                    var res = await GetUserConnectionsPage(user, page);
                    if (res != null && res.Count > 0)
                    {
                        ret.AddRange(res);

                        Log?.Invoke(this, new MySpaceLogEventArgs
                        {
                            Message = $"Crawled {ret.Count}/{maxConnectionCount} connections",
                            Username = user,
                            Guid = guid
                        });
                        page = page + 15;
                    }
                    else
                        tries--;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return ret;
        }
        public async Task<List<UserConnectionItem>> GetUserConnectionsPage(string username, int page = 0, int tries = 5)
        {
            while (tries > 0)
            {
                try
                {
                    var ret = new List<UserConnectionItem>();
                    var wr = ParseUserConnectionCrawlCurl(_connectionsCrawlingCulr.Replace("{user}", username), page);
                    var res = wr.GetResponse();
                    using (var responseStream = res.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        var tmp = reader.ReadToEnd();

                        if (tmp.Contains("nextStart\":-1"))
                            return new List<UserConnectionItem>();

                        if (!string.IsNullOrEmpty(tmp) && tmp != "{}")
                        {
                            var rs = JsonConvert.DeserializeObject<UserConnectionResponse>(tmp);

                            if (rs != null && !string.IsNullOrEmpty(rs.Html))
                            {
                                var doc = new HtmlDocument();
                                doc.LoadHtml(rs.Html);

                                foreach (var listNode in doc.DocumentNode.SelectNodes("//li/div[@class='mediaSquare x-large']"))
                                {

                                    var name = listNode.SelectSingleNode("./a/div[@class='nameplate']/h6")?.InnerText.Replace("\\t", "").Replace("\\n", "").Trim();
                                    var image = listNode.SelectSingleNode("./a/img")?.Attributes["src"].Value.Trim();
                                    var url = "https://myspace.com" + listNode.SelectSingleNode("./a").Attributes["href"].Value;

                                    ret.Add(new UserConnectionItem
                                    {
                                        Image = image,
                                        Url = url,
                                        Username = name
                                    });

                                }
                                return ret;
                            }
                        }
                        else
                        {
                            tries = tries - 1;
                            await Task.Delay(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tries = tries - 1;
                }
            }
            return new List<UserConnectionItem>();
        }

        private HttpWebRequest ParseUserConnectionCrawlCurl(string curl, int page = 0, int tries = 10)
        {
            var baseUrl = _baseUrlRegex.Match(curl).Groups["url"].Value;

            var webRequest = (HttpWebRequest)WebRequest.Create(baseUrl);
            webRequest.Method = "POST";
            foreach (Match headeRegexResult in _headerRegex.Matches(curl))
            {
                if (headeRegexResult.Success)
                {
                    var name = headeRegexResult.Groups["headerName"].Value;
                    var value = headeRegexResult.Groups["headerValue"].Value;

                    switch (name)
                    {
                        case "accept":
                            webRequest.Accept = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "content-type":
                            webRequest.ContentType = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "referer":
                            webRequest.Referer = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "user-agent":
                            webRequest.UserAgent = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "hash":
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                        default:
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                    }

                }
            }

            using (StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                requestWriter.Write($"start={page}");
            }



            return webRequest;
        }
        #endregion

        #region "Artist crawl stuff"
        private const string _artistCurl = @"curl 'https://myspace.com/ajax/page/search/artists?q={artist}' \
  -H 'accept: */*' \
  -H 'accept-language: de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7' \
  -H 'cache-control: no-cache' \
  -H 'client: persistentId=88092f65-0394-4c31-b351-cb9737026797&screenWidth=1920&screenHeight=1080&timeZoneOffsetHours=-2&visitId=65562bc3-2546-454b-9183-a6d70ead56df&windowWidth=1293&windowHeight=911' \
  -H 'content-type: application/x-www-form-urlencoded; charset=UTF-8' \
  -H 'cookie: playerControlTip=shown=true; persistent_id=pid%3D88092f65-0394-4c31-b351-cb9737026797%26llid%3D-1%26lprid%3D-1%26lltime%3D2024-07-01T13%253A49%253A52.210Z; geo=false; OptanonAlertBoxClosed=2024-07-10T16:56:10.505Z; visit_id=65562bc3-2546-454b-9183-a6d70ead56df; beacons_enabled=true; player=sequenceId=3&paused=true&currentTime=0&volume=0.2170138888888889&mute=false&shuffled=false&repeat=off&mode=queue&pinned=false&at=360&incognito=false&allowSkips=true&ccOn=false; OptanonConsent=isGpcEnabled=0&datestamp=Sun+Jul+28+2024+22%3A52%3A10+GMT%2B0200+(Mitteleurop%C3%A4ische+Sommerzeit)&version=202406.1.0&isIABGlobal=false&hosts=&landingPath=NotLandingPage&groups=C0003%3A1%2CC0001%3A1%2CC0002%3A1%2CC0004%3A1%2CSSPD_BG%3A1&AwaitingReconsent=false&browserGpcFlag=0&geolocation=DE%3BNW' \
  -H 'hash: MGRlODgwOTQ1NjcwMTg3ZsKiw6Jdw47Clj/DpgJSCF/DhSfClsKlwpTCmmrCmcOnwrZfek8uY8KGF8O3JgB6O8Ktw4nDusKpw7LCgMOgw4tBwqfChcO6w43ClsOcw4bCgyzCr8OZCcK7wrNRw4TDiWzDukTDrMOJMhUmecOsD8OfeMOnaCnCqsKfK8O0BjRBw5sRw4csR8KpdsKQFcO4UVzDhjLDpUnCuVsIwrFTw6bDpxDCqMK/wrkUwrsv' \
  -H 'origin: https://myspace.com' \
  -H 'pragma: no-cache' \
  -H 'priority: u=1, i' \
  -H 'referer: https://myspace.com/search/artists?q={artist}' \
  -H 'sec-ch-ua: ""Not/A)Brand"";v=""8"", ""Chromium"";v=""126"", ""Google Chrome"";v=""126""' \
  -H 'sec-ch-ua-mobile: ?0' \
  -H 'sec-ch-ua-platform: ""Windows""' \
  -H 'sec-fetch-dest: empty' \
  -H 'sec-fetch-mode: cors' \
  -H 'sec-fetch-site: same-origin' \
  -H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36' \
  -H 'x-requested-with: XMLHttpRequest' \
  --data-raw 'page=2&ssid=f8b5f520-b139-4995-88ba-29016f676f0d'";


        public async Task<List<ArtistItem>> CrawlArtists(string artist, string guid, int tries = 5)
        {
            var maxConnectionCount = 0;
            try
            {
                //maxConnectionCount = GetConnectionCountOfUser(user);
            }
            catch (Exception ex)
            {
                maxConnectionCount = -1;
            }
            var ret = new List<ArtistItem>();
            var page = 1;
            while (tries > 0)
            {
                try
                {
                    var res = await GetArtistPage(artist, page);
                    if (res != null && res.Count > 0)
                    {
                        ret.AddRange(res);

                        Log?.Invoke(this, new MySpaceLogEventArgs
                        {
                            Message = $"Crawled {ret.Count} artists",
                            Username = artist,
                            Guid = guid
                        });
                        page = page + 1;
                    }
                    else
                        tries--;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return ret;
        }
        public async Task<List<ArtistItem>> GetArtistPage(string artist, int page = 1, int tries = 5)
        {
            while (tries > 0)
            {
                try
                {
                    var ret = new List<ArtistItem>();
                    var wr = ParseArtistCurl(_artistCurl.Replace("{artist}", artist), page);
                    var res = wr.GetResponse();
                    using (var responseStream = res.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        var tmp = reader.ReadToEnd();

                        if (!string.IsNullOrEmpty(tmp) && tmp != "{}")
                        {
                            if (!string.IsNullOrEmpty(tmp))
                            {
                                var doc = new HtmlDocument();
                                doc.LoadHtml(tmp);

                                foreach (var listNode in doc.DocumentNode.SelectNodes("//li/div[@class='media ellipsis size_300']"))
                                {

                                    var name = listNode.SelectSingleNode("./div/h6")?.InnerText.Replace("\\t", "").Replace("\\n", "").Trim();
                                    var image = listNode.SelectSingleNode("./a/img").Attributes["src"].Value;
                                    var url = "https://myspace.com" + listNode.SelectSingleNode("./a").Attributes["href"].Value;

                                    ret.Add(new ArtistItem
                                    {
                                        Image = image,
                                        Url = url,
                                        Name = name
                                    });

                                }
                                return ret;
                            }
                        }
                        else
                        {
                            tries = tries - 1;
                            await Task.Delay(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    tries = tries - 1;
                }
            }
            return new List<ArtistItem>();
        }

        private HttpWebRequest ParseArtistCurl(string curl, int page = 1)
        {
            var baseUrl = _baseUrlRegex.Match(curl).Groups["url"].Value;

            var webRequest = (HttpWebRequest)WebRequest.Create(baseUrl);
            webRequest.Method = "POST";
            foreach (Match headeRegexResult in _headerRegex.Matches(curl))
            {
                if (headeRegexResult.Success)
                {
                    var name = headeRegexResult.Groups["headerName"].Value;
                    var value = headeRegexResult.Groups["headerValue"].Value;

                    switch (name)
                    {
                        case "accept":
                            webRequest.Accept = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "content-type":
                            webRequest.ContentType = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "referer":
                            webRequest.Referer = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "user-agent":
                            webRequest.UserAgent = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "hash":
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                        default:
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                    }

                }
            }

            var ssid = _songContentRegex.Match(curl).Groups["ssid"].Value;
            using (StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                requestWriter.Write($"page={page}&ssid={ssid}");
            }



            return webRequest;
        }
        #endregion

        #region "Artist Song crawl stuff"
        private const string _artistMusicCurl = @"curl 'https://myspace.com/ajax/{artist}/music/songs' \
  -H 'accept: */*' \
  -H 'accept-language: de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7' \
  -H 'cache-control: no-cache' \
  -H 'client: persistentId=88092f65-0394-4c31-b351-cb9737026797&screenWidth=1920&screenHeight=1080&timeZoneOffsetHours=-2&visitId=0f68c622-23f9-4b33-84b1-942bb1ca53ac&windowWidth=1293&windowHeight=911' \
  -H 'content-type: application/x-www-form-urlencoded; charset=UTF-8' \
  -H 'cookie: playerControlTip=shown=true; persistent_id=pid%3D88092f65-0394-4c31-b351-cb9737026797%26llid%3D-1%26lprid%3D-1%26lltime%3D2024-07-01T13%253A49%253A52.210Z; geo=false; OptanonAlertBoxClosed=2024-07-10T16:56:10.505Z; visit_id=0f68c622-23f9-4b33-84b1-942bb1ca53ac; beacons_enabled=true; player=sequenceId=3&paused=true&currentTime=0&volume=0.2170138888888889&mute=false&shuffled=false&repeat=off&mode=queue&pinned=false&at=360&incognito=false&allowSkips=true&ccOn=false; OptanonConsent=isGpcEnabled=0&datestamp=Sat+Jul+27+2024+20%3A08%3A46+GMT%2B0200+(Mitteleurop%C3%A4ische+Sommerzeit)&version=202406.1.0&isIABGlobal=false&hosts=&landingPath=NotLandingPage&groups=C0003%3A1%2CC0001%3A1%2CC0002%3A1%2CC0004%3A1%2CSSPD_BG%3A1&AwaitingReconsent=false&browserGpcFlag=0&geolocation=DE%3BNW' \
  -H 'hash: ZDRjNTg5MzcwMmYzZTRmMQJpNsOpQTM7w4zClMOXw7EMw5LCicKRwrNQKsKXL8OcQ0ZuwrvDjkLCu8K0w7cwF2vCtsO3DcOOBMKgRsOEbELCjsKpM8O7DcOkwrx8w5zDmsKid8OhYFFoLEduwqpTwrvDscK9P3FPN8OJwojDl8OjTSQuw6g9w6DDklkHwoXCkMOTwrttV0YTR8KIPMO5w5PDjx/Dsg0bwo56wqXCml5xwoY2bHY%3D' \
  -H 'origin: https://myspace.com' \
  -H 'pragma: no-cache' \
  -H 'priority: u=1, i' \
  -H 'referer: https://myspace.com/entershikari/{artist}/songs' \
  -H 'sec-ch-ua: ""Not/A)Brand"";v=""8"", ""Chromium"";v=""126"", ""Google Chrome"";v=""126""' \
  -H 'sec-ch-ua-mobile: ?0' \
  -H 'sec-ch-ua-platform: ""Windows""' \
  -H 'sec-fetch-dest: empty' \
  -H 'sec-fetch-mode: cors' \
  -H 'sec-fetch-site: same-origin' \
  -H 'user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36' \
  -H 'x-requested-with: XMLHttpRequest' \
  --data-raw 'start=100&max=50'";


        public async Task<List<SongItem>> CrawlSongsByArtist(string artist, string guid)
        {
            artist = HttpUtility.HtmlEncode(artist);
            var tmp = _artistMusicCurl.Replace("{artist}", artist);
            return await CrawlArtistSongs(tmp, artist, guid);
        }

        public async Task<List<SongItem>> CrawlArtistSongs(string curl, string artist, string guid, int tries = 10)
        {
            var ret = new List<SongItem>();
            var page = 0;
            while (true)
            {
                try
                {
                    var res = await CrawlArtistSongPage(curl, artist, guid, page, tries);

                    if (res?.Count > 0)
                    {
                        _maxPageTries = 2;
                        ret.AddRange(res);

                        //lbSongs.Invoke((MethodInvoker)delegate { lbSongs.Items.AddRange(res.ToArray()); });
                        page = page + 1;
                    }
                    else
                    {
                        if (_maxPageTries == 0)
                            break;
                        else
                        {
                            page = page + 1;
                            tries = 10;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex = ex;
                    await Task.Delay(1000);
                }
            }
            return ret;
        }

        private async Task<List<SongItem>> CrawlArtistSongPage(string text, string artist, string guid, int page = 1, int tries = 10)
        {

            while (tries > 0)
            {
                try
                {

                    Log?.Invoke(this, new MySpaceLogEventArgs
                    {
                        Message = $"Crawling page '{page}' | Tries left: {tries}",
                        Username = artist,
                        SearchTerm = artist,
                        Guid = guid
                    });
                    var ret = new List<SongItem>();
                    var wr = ParseArtistSongCurl(text, page);
                    var res = await wr.GetResponseAsync();
                    using (var responseStream = res.GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        var tmp = reader.ReadToEnd();

                        if (!string.IsNullOrEmpty(tmp) && tmp != "\t")
                        {
                            var doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(tmp);

                            foreach (var listNode in doc.DocumentNode.SelectNodes("//li/div[@class='flex']"))
                            {
                                var titleNode = listNode.SelectSingleNode("./div[@class='title']");
                                var artistNode = listNode.SelectSingleNode("./div[@class='artist']");
                                var albumNode = listNode.SelectSingleNode("./div[@class='album']");
                                var dateNode = listNode.SelectSingleNode("./div[@class='date']");
                                var durationNode = listNode.SelectSingleNode("./div[@class='duration']");


                                ret.Add(new SongItem
                                {
                                    Album = albumNode?.InnerText.Trim().Replace("\n", ""),
                                    Artist = artistNode?.InnerText.Trim().Replace("\n", ""),
                                    Date = dateNode?.InnerText.Trim().Replace("\n", ""),
                                    Duration = durationNode?.InnerText.Trim().Replace("\n", ""),
                                    Url = "https://myspace.com" + titleNode?.SelectSingleNode("./a")?.Attributes["href"]?.Value,
                                    Title = titleNode?.InnerText.Trim()
                                });

                            }
                            return ret;
                        }
                        else
                        {
                            tries = tries - 1;
                            await Task.Delay(1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    //Log?.Invoke(this, new MySpaceLogEventArgs
                    //{
                    //    Message = $"Error: {ex}",
                    //    Username = term,
                    //     SearchTerm = term,
                    //     Guid = guid
                    //});
                }
            }
            _maxPageTries = _maxPageTries - 1;
            return null;
        }
        private HttpWebRequest ParseArtistSongCurl(string curl, int page = 1)
        {
            var baseUrl = _baseUrlRegex.Match(curl).Groups["url"].Value;

            var webRequest = (HttpWebRequest)WebRequest.Create(baseUrl);
            webRequest.Method = "POST";
            foreach (Match headeRegexResult in _headerRegex.Matches(curl))
            {
                if (headeRegexResult.Success)
                {
                    var name = headeRegexResult.Groups["headerName"].Value;
                    var value = headeRegexResult.Groups["headerValue"].Value;

                    switch (name)
                    {
                        case "accept":
                            webRequest.Accept = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "content-type":
                            webRequest.ContentType = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "referer":
                            webRequest.Referer = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "user-agent":
                            webRequest.UserAgent = headeRegexResult.Groups["headerValue"].Value;
                            break;
                        case "hash":
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                        default:
                            webRequest.Headers.Add(headeRegexResult.Groups["headerName"].Value, headeRegexResult.Groups["headerValue"].Value);
                            break;
                    }

                }
            }

            var ssid = _songContentRegex.Match(curl).Groups["ssid"].Value;
            using (StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                requestWriter.Write($"start={page * 50}&max=50");
            }



            return webRequest;
        }
        #endregion
    }
}
