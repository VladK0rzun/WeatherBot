using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using WeatherBot.Data;
using WeatherBot.Service;

var builder = WebApplication.CreateBuilder(args);

// ��������� �����������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ����������� ���� ������
builder.Services.AddSingleton<DbContext>();

// ����������� ��������
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<TelegramBotService>();

// ����������� TelegramBotClient
var botToken = builder.Configuration["BotConfiguration:BotToken"];
if (string.IsNullOrEmpty(botToken))
{
    throw new Exception("BotToken �� ������! ������� appsettings.json");
}
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

var app = builder.Build();

// ��������� Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ������ ����
var botService = app.Services.GetRequiredService<TelegramBotService>();
var botClient = app.Services.GetRequiredService<ITelegramBotClient>();

botClient.StartReceiving(
    async (client, update, token) => await botService.HandleUpdateAsync(update),
    async (client, exception, token) => Console.WriteLine($"������ � ����: {exception.Message}")
);

// ������������� ���� ������
var dbContext = app.Services.GetRequiredService<DbContext>();
await DbInitializer.InitializeAsync(dbContext);

app.Run();
