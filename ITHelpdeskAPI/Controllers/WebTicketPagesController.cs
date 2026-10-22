
using System.Security.Claims;
using ITHelpdeskAPI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ITHelpdeskAPI.Controllers;

[Authorize(
    AuthenticationSchemes =
        CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "technician,manager")]
[Route("WebTicketPages")]
public class WebTicketPagesController : Controller
{
    private readonly TicketService _ticketService;
    private readonly IConfiguration _configuration;

    public WebTicketPagesController(
        TicketService ticketService,
        IConfiguration configuration)
    {
        _ticketService = ticketService;
        _configuration = configuration;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        return await LoadPage("Tickets");
    }

    [HttpGet("Assign")]
    [Authorize(
        AuthenticationSchemes =
            CookieAuthenticationDefaults.AuthenticationScheme,
        Roles = "technician,manager")]
    public async Task<IActionResult> Assign()
    {
        return await LoadPage("Assign Tickets");
    }

    private async Task<IActionResult> LoadPage(string heading)
    {
        string? userIdText =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        string? role =
            User.FindFirstValue(ClaimTypes.Role);

        string? email =
            User.FindFirstValue(ClaimTypes.Email);

        if (!Guid.TryParse(userIdText, out Guid userId) ||
            string.IsNullOrWhiteSpace(role))
        {
            return Challenge(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        var tickets = await _ticketService.GetTicketsAsync(
            userId,
            role);

        var rows = tickets.Select(ticket =>
        {
            var type = ticket.GetType();

            string GetValue(string name)
            {
                return type.GetProperty(name)?
                    .GetValue(ticket)?.ToString() ?? "";
            }

            return new
            {
                Id = GetValue("Id"),
                ReferenceNumber = GetValue("ReferenceNumber"),
                Title = GetValue("Title"),
                Priority = GetValue("Priority"),
                Status = GetValue("Status"),
                AssignedTo = GetValue("AssignedTo")
            };
        }).ToList();

        var technicians = new List<(Guid Id, string Name)>();

        if (role.Equals(
            "manager",
            StringComparison.OrdinalIgnoreCase))
        {
            string connectionString =
                _configuration.GetConnectionString("Supabase")
                ?? throw new InvalidOperationException(
                    "Supabase connection string is missing.");

            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT
                    id,
                    COALESCE(
                        NULLIF(full_name, ''),
                        email,
                        id::text
                    )
                FROM public.profiles
                WHERE role = 'technician'
                ORDER BY full_name NULLS LAST, email;
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                technicians.Add((
                    reader.GetGuid(0),
                    reader.GetString(1)
                ));
            }
        }

        ViewBag.Heading = heading;
        ViewBag.UserRole = role;
        ViewBag.UserEmail = email ?? "";
        ViewBag.CurrentUserId = userId.ToString();
        ViewBag.Tickets = rows;
        ViewBag.Technicians = technicians;

        return View("Index");
    }
}
