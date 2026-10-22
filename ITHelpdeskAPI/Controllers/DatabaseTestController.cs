
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ITHelpdeskAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseTestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DatabaseTestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var connectionString =
                    _configuration.GetConnectionString("Supabase");

                await using var connection =
                    new NpgsqlConnection(connectionString);

                await connection.OpenAsync();

                await using var command =
                    new NpgsqlCommand("SELECT 1", connection);

                var result = await command.ExecuteScalarAsync();

                return Ok(new
                {
                    message = "Connected to Supabase successfully!",
                    result = result
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("DATABASE ERROR:");
                Console.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    message = "Database connection failed. Check the Visual Studio console."
                });
            }
        }
    }
}
