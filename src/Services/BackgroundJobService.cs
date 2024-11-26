using Lostwave.Tools.Models;
using Newtonsoft.Json;

namespace Lostwave.Tools.Services
{
    public class BackgroundJobService : IHostedService
    {
        private readonly ILogger<BackgroundJobService> _logger;


        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists("jobs"))
                Directory.CreateDirectory("jobs");
            if (!Directory.Exists("results"))
                Directory.CreateDirectory("results");
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");


            var fileSystemWatcher = new FileSystemWatcher("jobs");
            fileSystemWatcher.Created += OnNewJobFileCreated;
            fileSystemWatcher.EnableRaisingEvents = true;

            foreach (var file in Directory.GetFiles("jobs", "*.json"))
            {
                var content = File.ReadAllText(file);

                File.Delete(file);
                File.WriteAllText(file, content);
            }


            _ = DoWorkAsync(cancellationToken);


            return Task.CompletedTask;
        }

        private async void OnNewJobFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Triggered");
            var myspaceService = new MySpaceCrawlingService();
            //var songFinderService = new SongFinderService(soulSeekClient);
            //var fingerPrintService = new FingerprintComparingService();
            //var lastFm = new LastFmService();
            try
            {
                myspaceService.Log += OnLog;
                //lastFm.Log += OnLastFmLog;
                //fingerPrintService.Log += OnYoutubeLog;
                //songFinderService.Log += OnSongFinderLog;

                await Task.Delay(1000);
                var json = File.ReadAllText(e.FullPath);
                var job = JsonConvert.DeserializeObject<Job>(json);
                if (job == null)
                    return;

                switch (job.Name)
                {
                    case "myspace_song_crawler":
                        var songCrawlerResult = await myspaceService.CrawlSongsByTerm(job.Parameter["searchTerm"], job.Parameter["guid"]);
                        WriteSongJsonToFile(myspaceService, songCrawlerResult, $"results/song_{job.Parameter["searchTerm"].Replace(" ", "_")}.json", job.Parameter["searchTerm"], job.Parameter["guid"]);
                        //WriteSongJsonToFile(myspaceService, await myspaceService.CrawlSongsByTerm(job.Parameter["searchTerm"], job.Parameter["guid"]), $"results/song_{job.Parameter["searchTerm"].Replace(" ", "_")}.json", job.Parameter["searchTerm"], job.Parameter["guid"]);
                        break;
                    case "myspace_artist_crawler":
                        WriteArtistJsonToFile(myspaceService, await myspaceService.CrawlArtists(job.Parameter["artist"], job.Parameter["guid"]), $"results/artists_{job.Parameter["artist"].Replace(" ", "_")}.json", job.Parameter["artist"], job.Parameter["guid"]);
                        break;
                    case "myspace_artist_song_crawler":
                        WriteArtistSongsJsonToFile(myspaceService, await myspaceService.CrawlSongsByArtist(job.Parameter["artist"], job.Parameter["guid"]), $"results/artist_songs_{job.Parameter["artist"].Replace(" ", "_")}.json", job.Parameter["artist"], job.Parameter["guid"]);
                        break;
                    case "myspace_user_connection_crawler":
                        var username = job.Parameter["username"];
                        var guid = job.Parameter["guid"];
                        WriteUserConnectionJsonToFile(myspaceService, await myspaceService.GetUserConnection(username, guid), $"results/userconnection_{username}.json", username, guid);
                        break;
                        //case "youtube_fingerprint":
                        //    WriteFingerprintJsonToFile(fingerPrintService, await fingerPrintService.Compare(job.Parameter["sourceVideo"], job.Parameter["targetLink"], job.Parameter["guid"]), $"results/fingerprint_{job.Parameter["guid"]}.json", job.Parameter["sourceVideo"], job.Parameter["targetLink"], job.Parameter["guid"]);
                        //    break;
                        //case "song_finder":
                        //    WriteSongFinderJsonToFile(songFinderService, await songFinderService.Search(job.Parameter["searchTerm"], job.Parameter["sources"].Split(','), job.Parameter["guid"]), $"results/songfinder_{job.Parameter["searchTerm"].Replace(" ", "_")}.json", job.Parameter["searchTerm"], job.Parameter["guid"]);
                        //    break;
                        //case "last_fm":
                        //    WriteLastFmJsonToFile(lastFm, await lastFm.GetByTerm(job.Parameter["searchTerm"], job.Parameter["guid"]), $"results/lastfm_{job.Parameter["searchTerm"].Replace(" ", "_")}.json", job.Parameter["searchTerm"], job.Parameter["guid"]);
                        //    break;
                }
            }
            catch
            {
            }
            finally
            {
                File.Delete(e.FullPath);
                await Task.Delay(10000);
                //myspaceService.Log -= OnLog;
                //fingerPrintService.Log -= OnYoutubeLog;
                //songFinderService.Log -= OnSongFinderLog;
            }
        }

        private void WriteSongJsonToFile(MySpaceCrawlingService myspaceService, List<SongItem> ret, string fileName, string searchTerm, string guid)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(ret));
            myspaceService.Log?.Invoke(null, new MySpaceLogEventArgs
            {
                Message = $"FINISHED: Found {ret.Count} songs",
                Username = searchTerm,
                SearchTerm = searchTerm,
                Guid = guid
            });
        }

        void WriteArtistJsonToFile(MySpaceCrawlingService myspaceService, List<ArtistItem> ret, string fileName, string artist, string guid)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(ret));
            myspaceService.Log?.Invoke(myspaceService, new MySpaceLogEventArgs
            {
                Message = $"FINISHED: Found {ret.Count} songs",
                Username = artist,
                SearchTerm = artist,
                Guid = guid
            });
        }

        void WriteArtistSongsJsonToFile(MySpaceCrawlingService myspaceService, List<SongItem> ret, string fileName, string artist, string guid)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(ret));
            myspaceService.Log?.Invoke(myspaceService, new MySpaceLogEventArgs
            {
                Message = $"FINISHED: Found {ret.Count} songs",
                Username = artist,
                SearchTerm = artist,
                Guid = guid
            });
        }
        void WriteUserConnectionJsonToFile(MySpaceCrawlingService myspaceService, List<UserConnectionItem> ret, string fileName, string username, string guid)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(ret));
            myspaceService.Log?.Invoke(myspaceService, new MySpaceLogEventArgs
            {
                Message = $"FINISHED: Found {ret.Count} connections",
                Username = username,
                Guid = guid
            });
        }

        string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Job service started");
                await Task.Delay(5000, cancellationToken); // Simuliert Arbeit
            }
        }

        void OnLog(object? sender, MySpaceLogEventArgs e)
        {
            Console.WriteLine(e.Message);
            if (!Directory.Exists($"logs/{e.Username.Replace(" ", "_")}"))
                Directory.CreateDirectory($"logs/{e.Username.Replace(" ", "_")}");
            using (var sw = new StreamWriter($"./logs/{e.Username.Replace(" ", "_")}/log_{e.Username.Replace(" ", "_")}_{e.Guid}.log", true))
            {
                sw.WriteLine(e.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
