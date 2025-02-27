using Dapper;
using Microsoft.AspNetCore.Mvc;
using WeatherBot.Data;
using WeatherBot.Models;

namespace WeatherBot.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DbContext _context;

        public UserController(DbContext context)
        {
            _context = context;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(long userId)
        {
            using var connection = _context.CreateConnection();
            var user = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @UserId", new { UserId = userId });

            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
    }
}
