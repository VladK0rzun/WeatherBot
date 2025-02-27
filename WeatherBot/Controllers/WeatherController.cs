using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using WeatherBot.Service;
using WeatherBot.Data;
using WeatherBot.Models;
using Dapper;

namespace WeatherBot.Controllers
{
    [Route("api/weather")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly DbContext _dbContext;
        private readonly WeatherService _weatherService;
        private readonly ITelegramBotClient _botClient;

        public WeatherController(DbContext dbContext, WeatherService weatherService, ITelegramBotClient botClient)
        {
            _dbContext = dbContext;
            _weatherService = weatherService;
            _botClient = botClient;
        }

        [HttpPost("sendWeatherToAll")]
        public async Task<IActionResult> SendWeatherToAll([FromBody] WeatherRequest request)
        {
            using var connection = _dbContext.CreateConnection();

            // Получаем пользователей
            List<long> userIds = request.UserId.HasValue
                ? new List<long> { request.UserId.Value }
                : (await connection.QueryAsync<long>("SELECT Id FROM Users")).AsList();

            if (userIds.Count == 0)
            {
                return NotFound("No users found.");
            }

            // Получаем погоду
            string weather = await _weatherService.GetWeatherAsync(request.City);

            // Отправляем погоду
            foreach (var userId in userIds)
            {
                await _botClient.SendTextMessageAsync(userId, $"Weather in {request.City}:\n{weather}");
            }

            return Ok(new { message = "Weather sent successfully!", usersNotified = userIds.Count });
        }
    }
}
