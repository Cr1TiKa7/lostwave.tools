namespace Lostwave.Tools.Models
{
    public class CrawlItem
    {
        public string SearchTerm { get; set; }
        public string Type { get; set; }
        public string CrawledAt { get; set; }
        public string FilePath { get; set; }
    }

    public class YoutubeCrawlItem
    {
        public string Source { get; set; }
        public string SourceTitle { get; set; }
        public DateTime CrawledAt { get; set; }
        public string FilePath { get; set; }
        public int Matches { get; set; }
    }
}
