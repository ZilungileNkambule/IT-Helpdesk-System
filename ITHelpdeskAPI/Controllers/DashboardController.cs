

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
public class DashboardController : Controller
{
    private readonly TicketService _ticketService;
    private readonly IConfiguration _configuration;

    public DashboardController(
        TicketService ticketService,
        IConfiguration configuration)
    {
        _ticketService = ticketService;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        // 1. Get the logged-in user's details
        var userIdText =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var role =
            User.FindFirstValue(ClaimTypes.Role);

        var email =
            User.FindFirstValue(ClaimTypes.Email);

        if (!Guid.TryParse(userIdText, out var userId) ||
            string.IsNullOrWhiteSpace(role))
        {
            return Challenge(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // 2. Retrieve tickets from the existing service
        var tickets = await _ticketService.GetTicketsAsync(
            userId, role);

        // 3. Prepare tickets for the dashboard
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

        // 4. Calculate dashboard statistics
        ViewBag.TotalTickets = rows.Count;

        ViewBag.OpenTickets =
            rows.Count(t => t.Status == "Open");

        ViewBag.InProgressTickets =
            rows.Count(t => t.Status == "In Progress");

        ViewBag.ResolvedTickets =
            rows.Count(t => t.Status == "Resolved");

        // 5. Retrieve technicians for manager assignments
        var technicians = new List<(Guid Id, string Name)>();

        if (role == "manager")
        {
            var connectionString =
                _configuration.GetConnectionString("Supabase");

            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT id, COALESCE(
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

        // 6. Send data to the Razor view
        ViewBag.UserRole = role;
        ViewBag.UserEmail = email;
        ViewBag.Tickets = rows;
        ViewBag.Technicians = technicians;
        ViewBag.CurrentUserId = userId.ToString();

        // 7. Render the dashboard only after loading everything
        return View();
    }
}

