
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ITHelpdeskAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CategoriesController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var connectionString =
                _configuration.GetConnectionString("Supabase");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(500,
                    "Database configuration is missing.");
            }

            var categories = new List<object>();

            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT id, name
                FROM public.ticket_categories
                ORDER BY name;
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categories.Add(new
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1)
                });
            }

            return Ok(categories);
        }
    }
}
