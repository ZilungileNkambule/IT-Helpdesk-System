
using System.Security.Claims;
using ITHelpdeskAPI.Models;
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
[Route("WebTickets")]
public class WebTicketsController : Controller
{
    private readonly TicketService _tickets;
    private readonly IConfiguration _configuration;

    public WebTicketsController(
        TicketService tickets,
        IConfiguration configuration)
    {
        _tickets = tickets;
        _configuration = configuration;
    }

    [HttpPost("SelfAssign")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "technician",
        AuthenticationSchemes =
            CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<IActionResult> SelfAssign(Guid ticketId)
    {
        if (!Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId))
            return Forbid();

        await _tickets.SelfAssignTicketAsync(ticketId, userId);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost("Assign")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "manager",
        AuthenticationSchemes =
            CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Assign(
        Guid ticketId, Guid technicianId)
    {
        await _tickets.AssignTicketAsync(ticketId, technicianId);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost("UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        Guid ticketId,
        string status,
        string? resolutionNotes)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId))
            return Forbid();

        if (status is not
            ("Open" or "In Progress" or "Resolved" or "Closed"))
            return BadRequest("Invalid status.");

        // A technician may only update tickets assigned to them.
        // Managers may update any ticket.
        if (role != "manager")
        {
            await using var connection = new NpgsqlConnection(
                _configuration.GetConnectionString("Supabase"));

            await connection.OpenAsync();

            await using var check = new NpgsqlCommand(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM public.tickets
                    WHERE id = @ticketId
                      AND assigned_to = @userId
                );
                """, connection);

            check.Parameters.AddWithValue("ticketId", ticketId);
            check.Parameters.AddWithValue("userId", userId);

            var assigned = (bool)(await check.ExecuteScalarAsync()
                ?? false);

            if (!assigned)
                return Forbid();
        }

        if (status == "Resolved" &&
            string.IsNullOrWhiteSpace(resolutionNotes))
        {
            return BadRequest(
                "Resolution notes are required.");
        }

        await _tickets.UpdateTicketStatusAsync(
            ticketId, status, resolutionNotes);

        return RedirectToAction("Index", "Dashboard");
    }
}
