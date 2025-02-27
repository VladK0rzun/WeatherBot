namespace WeatherBot.Models
{
    public class WeatherHistory
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public string City { get; set; }
        public string WeatherData { get; set; }
        public string RequestDate { get; set; }
    }
}
