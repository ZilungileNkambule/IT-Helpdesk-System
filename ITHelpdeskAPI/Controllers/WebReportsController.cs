
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ITHelpdeskAPI.Controllers;

[Authorize(
    AuthenticationSchemes =
        CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "manager")]
public class WebReportsController : Controller
{
    private readonly IConfiguration _config;

    public WebReportsController(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var counts = new Dictionary<string, int>();

        await using var connection = new NpgsqlConnection(
            _config.GetConnectionString("Supabase"));

        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT status, COUNT(*)::int
            FROM public.tickets
            GROUP BY status;
            """, connection);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            counts[reader.GetString(0)] = reader.GetInt32(1);
        }

        return View(counts);
    }
}
