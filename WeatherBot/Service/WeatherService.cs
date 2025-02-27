using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace WeatherBot.Service
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WeatherService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["BotConfiguration:WeatherApiKey"];
        }

        public async Task<string> GetWeatherAsync(string city, string languageCode = "en")
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric&lang={languageCode}";
            var response = await _httpClient.GetStringAsync(url);
            var weather = JsonSerializer.Deserialize<JsonElement>(response);

            var messages = GetLocalizedMessages(languageCode);

            string description = weather.GetProperty("weather")[0].GetProperty("description").GetString();

            return $"{messages["city"]} {city}\n" +
                   $"{messages["temperature"]} {weather.GetProperty("main").GetProperty("temp")}°C\n" +
                   $"{messages["description"]} {description}";
        }

        private Dictionary<string, string> GetLocalizedMessages(string languageCode)
        {
            return languageCode switch
            {
                "uk" => new Dictionary<string, string>
            {
                { "city", "🌍 Місто:" },
                { "temperature", "🌡 Температура:" },
                { "description", "🌤 Опис:" }
            },
                "ru" => new Dictionary<string, string>
            {
                { "city", "🌍 Город:" },
                { "temperature", "🌡 Температура:" },
                { "description", "🌤 Описание:" }
            },
                _ => new Dictionary<string, string> // По умолчанию английский
            {
                { "city", "🌍 City:" },
                { "temperature", "🌡 Temperature:" },
                { "description", "🌤 Description:" }
            }
            };
        }
    }
}