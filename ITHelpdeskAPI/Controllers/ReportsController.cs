
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace ITHelpdeskAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly string _connectionString;

        public ReportsController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("Supabase")
                ?? throw new InvalidOperationException(
                    "Supabase connection string is missing.");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var userIdText =
                User.FindFirst("sub")?.Value
                ?? User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdText, out var userId))
                return Unauthorized();

            if (from.HasValue && to.HasValue && from > to)
                return BadRequest(
                    "The start date must be before the end date.");

            await using var connection =
                new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            // Verify the user's role using the database.
            const string roleSql = """
                SELECT role
                FROM public.profiles
                WHERE id = @userId;
                """;

            await using (var roleCommand =
                new NpgsqlCommand(roleSql, connection))
            {
                roleCommand.Parameters.AddWithValue(
                    "userId", userId);

                var role = await roleCommand
                    .ExecuteScalarAsync() as string;

                if (role != "manager")
                    return Forbid();
            }

            const string reportSql = """
                SELECT
                    COUNT(*) AS total,
                    COUNT(*) FILTER
                        (WHERE status = 'Open') AS open,
                    COUNT(*) FILTER
                        (WHERE status = 'In Progress') AS in_progress,
                    COUNT(*) FILTER
                        (WHERE status = 'Resolved') AS resolved,
                    COUNT(*) FILTER
                        (WHERE status = 'Closed') AS closed,
                    AVG(
                        EXTRACT(EPOCH FROM
                            (resolved_at - created_at)
                        ) / 3600.0
                    ) FILTER (
                        WHERE resolved_at IS NOT NULL
                    ) AS average_resolution_hours
                FROM public.tickets
                WHERE (@fromDate IS NULL
                    OR created_at >= @fromDate)
                  AND (@toDate IS NULL
                    OR created_at < @toDate);
                """;

            await using var command =
                new NpgsqlCommand(reportSql, connection);

            command.Parameters.AddWithValue(
                "fromDate",
                NpgsqlTypes.NpgsqlDbType.TimestampTz,
                from.HasValue
                    ? from.Value.ToUniversalTime()
                    : DBNull.Value);

            command.Parameters.AddWithValue(
                "toDate",
                NpgsqlTypes.NpgsqlDbType.TimestampTz,
                to.HasValue
                    ? to.Value.ToUniversalTime()
                    : DBNull.Value);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return StatusCode(500, "Report unavailable.");

            return Ok(new
            {
                TotalTickets = reader.GetInt64(0),
                OpenTickets = reader.GetInt64(1),
                InProgressTickets = reader.GetInt64(2),
                ResolvedTickets = reader.GetInt64(3),
                ClosedTickets = reader.GetInt64(4),
                AverageResolutionHours =
                    reader.IsDBNull(5)
                        ? (double?)null
                        : reader.GetDouble(5)
            });
        }
    }
}
