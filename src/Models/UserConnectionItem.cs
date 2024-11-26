namespace Lostwave.Tools.Models
{
    public class UserConnectionItem
    {
        public string Username { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
    }

    public class UserConnectionResponse
    {
        public string Html { get; set; }
        public int NextStart { get; set; }
    }
}
