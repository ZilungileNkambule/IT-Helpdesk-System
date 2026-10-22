
using ITHelpdeskAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;
using ITHelpdeskAPI.DTOs;

namespace ITHelpdeskAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly TicketService _ticketService;
        private readonly IConfiguration _configuration;

        public TicketsController(
            TicketService ticketService,
            IConfiguration configuration)
        {
            _ticketService = ticketService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            // Get the user ID from the validated JWT.
            var userIdText =
                User.FindFirst("sub")?.Value
                ?? User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdText, out var userId))
            {
                return Unauthorized(
                    "Invalid user identity.");
            }

            // Retrieve the role from the database,
            // not from user-supplied input.
            var connectionString =
                _configuration.GetConnectionString("Supabase");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(500,
                    "Database configuration is missing.");
            }

            string? role;

            await using (var connection =
                new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                const string sql = """
                    SELECT role
                    FROM public.profiles
                    WHERE id = @userId;
                    """;

                await using var command =
                    new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "userId", userId);

                role = await command.ExecuteScalarAsync()
                    as string;
            }

            if (role is not ("staff" or "technician" or "manager"))
            {
                return Forbid();
            }

            var tickets =
                await _ticketService.GetTicketsAsync(
                    userId, role);

            return Ok(tickets);
        }


        [HttpPost]
        public async Task<IActionResult> CreateTicket(
            [FromBody] CreateTicketDto dto)
        {
            var userIdText =
                User.FindFirst("sub")?.Value
                ?? User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

            if (!Guid.TryParse(userIdText, out var userId))
            {
                return Unauthorized("Invalid user identity.");
            }

            try
            {
                var ticket = await _ticketService
                    .CreateTicketAsync(dto, userId);

                return Created(
                    $"/api/tickets/{ticket.Id}",
                    ticket);
            }
            catch (PostgresException ex)
                when (ex.SqlState == "23503")
            {
                return BadRequest(
                    "Invalid ticket category or user profile.");
            }
        }


        [HttpPut("{id:guid}/assign")]
        public async Task<IActionResult> AssignTicket(
            Guid id, AssignTicketDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (await _ticketService.GetRoleAsync(userId)
                != "manager")
                return Forbid();

            if (!dto.TechnicianId.HasValue)
                return BadRequest("Technician is required.");

            var updated = await _ticketService.AssignTicketAsync(
                id, dto.TechnicianId.Value);

            return updated
                ? Ok(new { message = "Ticket assigned." })
                : NotFound("Ticket or technician not found.");
        }

        [HttpPut("{id:guid}/self-assign")]
        public async Task<IActionResult> SelfAssignTicket(Guid id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (await _ticketService.GetRoleAsync(userId)
                != "technician")
                return Forbid();

            var updated = await _ticketService
                .SelfAssignTicketAsync(id, userId);

            return updated
                ? Ok(new { message = "Ticket assigned to you." })
                : Conflict("Ticket is unavailable for self-assignment.");
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(
            Guid id, UpdateTicketStatusDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var role = await _ticketService.GetRoleAsync(userId);

            if (role is not ("technician" or "manager"))
                return Forbid();

            if (dto.Status == "Resolved" &&
                string.IsNullOrWhiteSpace(dto.ResolutionNotes))
            {
                return BadRequest(
                    "Resolution notes are required.");
            }

            var updated = await _ticketService.UpdateTicketStatusAsync(
                id, dto.Status, dto.ResolutionNotes);

            return updated
                ? Ok(new { message = "Ticket status updated." })
                : NotFound();
        }

        private bool TryGetUserId(out Guid userId)
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

            return Guid.TryParse(claim, out userId);
        }


    }
}
