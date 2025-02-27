using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Data;
using System.Text.RegularExpressions;
using WeatherBot.Service;
using WeatherBot.Data;

namespace WeatherBot.Service
{
    using Telegram.Bot;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using Microsoft.Extensions.Logging;
    using Dapper;
    using System.Data;
    using System.Text.RegularExpressions;

    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly WeatherService _weatherService;
        private readonly DbContext _dbContext;
        private readonly ILogger<TelegramBotService> _logger;

        private static readonly Regex CityNameRegex = new Regex(@"^[a-zA-Zа-яА-ЯґҐєЄіІїЇ\s-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public TelegramBotService(ITelegramBotClient botClient, WeatherService weatherService, DbContext dbContext, ILogger<TelegramBotService> logger)
        {
            _botClient = botClient;
            _weatherService = weatherService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            try
            {
                if (update.Type != UpdateType.Message || update.Message?.Text == null)
                    return;

                var message = update.Message;
                long userId = message.Chat.Id;
                string text = message.Text.Trim();
                string telegramLanguage = message.From?.LanguageCode ?? "en"; // Язык Telegram
                string detectedLanguage = DetectLanguage(text) ?? telegramLanguage; // Определяем язык города

                _logger.LogInformation($"Received message from {userId} ({detectedLanguage}): {text}");

                using var connection = _dbContext.CreateConnection();

                // Проверяем, есть ли пользователь в базе
                var existingUser = await connection.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE Id = @UserId", new { UserId = userId });

                if (existingUser == null)
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO Users (Id, Username) VALUES (@Id, @Username)",
                        new { Id = userId, Username = message.Chat.Username ?? "Unknown" });
                }

                // Получаем локализованные сообщения
                var messages = GetLocalizedMessages(detectedLanguage);

                if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    await _botClient.SendTextMessageAsync(userId, messages["start"]);
                }
                else if (CityNameRegex.IsMatch(text)) // Если текст - это название города
                {
                    string weather = await _weatherService.GetWeatherAsync(text, detectedLanguage);

                    // Сохранение истории запросов
                    await connection.ExecuteAsync(
                        "INSERT INTO WeatherHistory (UserId, City, WeatherData, RequestTime) VALUES (@UserId, @City, @WeatherData, @RequestTime)",
                        new { UserId = userId, City = text, WeatherData = weather, RequestTime = DateTime.UtcNow });

                    await _botClient.SendTextMessageAsync(userId, $"{messages["weather"]} {text}:\n{weather}");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(userId, messages["invalid_city"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Bot error: {ex.Message}");
            }
        }

        // Функция для определения языка по названию города
        private string? DetectLanguage(string text)
        {
            if (Regex.IsMatch(text, @"^[а-яА-ЯґҐєЄіІїЇ\s-]+$")) return "uk"; // Только украинские буквы
            if (Regex.IsMatch(text, @"^[a-zA-Z\s-]+$")) return "en"; // Только английские буквы
            return null; // Язык не определён
        }

        private Dictionary<string, string> GetLocalizedMessages(string languageCode)
        {
            return languageCode switch
            {
                "uk" => new Dictionary<string, string>
            {
                { "start", "Добрий день. Введіть ваше місто." },
                { "weather", "Погода у" },
                { "invalid_city", "Некоректна назва міста. Будь ласка, введіть правильне місто." }
            },
                "ru" => new Dictionary<string, string>
            {
                { "start", "Добрый день. Введите ваш город." },
                { "weather", "Погода в" },
                { "invalid_city", "Некорректное название города. Пожалуйста, введите правильный город." }
            },
                _ => new Dictionary<string, string> // По умолчанию английский
            {
                { "start", "Good day. Please enter your city." },
                { "weather", "Weather in" },
                { "invalid_city", "Invalid city name. Please enter a valid city." }
            }
            };
        }
    }

}



//Hello! I am a bot that will show you the weather in any city. Just write /weather and the name of the city.