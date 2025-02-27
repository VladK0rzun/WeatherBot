using Dapper;

namespace WeatherBot.Data
{
    public class DbInitializer
    {
        public static async Task InitializeAsync(DbContext context)
        {
            using var connection = context.CreateConnection();

            var createUsersTable = @"
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
    BEGIN
        CREATE TABLE Users (
            Id BIGINT PRIMARY KEY,
            Username NVARCHAR(100) NOT NULL
        )
    END";

            var createHistoryTable = @"
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WeatherHistory' AND xtype='U')
    BEGIN
        CREATE TABLE WeatherHistory (
            Id INT IDENTITY PRIMARY KEY,
            UserId BIGINT,
            City NVARCHAR(100),
            WeatherData NVARCHAR(MAX),
            RequestTime DATETIME,
            FOREIGN KEY (UserId) REFERENCES Users(Id)
        )
    END";

            await connection.ExecuteAsync(createUsersTable);
            await connection.ExecuteAsync(createHistoryTable);
        }
    }
}
