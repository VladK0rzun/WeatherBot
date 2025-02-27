using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using WeatherBot.Data;
using WeatherBot.Service;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация базы данных
builder.Services.AddSingleton<DbContext>();

// Регистрация сервисов
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<TelegramBotService>();

// Регистрация TelegramBotClient
var botToken = builder.Configuration["BotConfiguration:BotToken"];
if (string.IsNullOrEmpty(botToken))
{
    throw new Exception("BotToken не найден! Проверь appsettings.json");
}
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

var app = builder.Build();

// Настройка Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Запуск бота
var botService = app.Services.GetRequiredService<TelegramBotService>();
var botClient = app.Services.GetRequiredService<ITelegramBotClient>();

botClient.StartReceiving(
    async (client, update, token) => await botService.HandleUpdateAsync(update),
    async (client, exception, token) => Console.WriteLine($"Ошибка в боте: {exception.Message}")
);

// Инициализация базы данных
var dbContext = app.Services.GetRequiredService<DbContext>();
await DbInitializer.InitializeAsync(dbContext);

app.Run();
