namespace Lostwave.Tools.Models
{
    public class Job
    {
        public string Name { get; set; }
        public Dictionary<string, string> Parameter { get; set; } = new Dictionary<string, string>();
    }
}
