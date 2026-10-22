
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace ITHelpdeskAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly string _connectionString;

    public NotificationsController(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException(
                "Database connection missing.");
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await using var connection =
            new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT id, title, message, is_read, created_at
            FROM public.notifications
            WHERE user_id = @userId
            ORDER BY created_at DESC;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("userId", userId.Value);

        var notifications = new List<object>();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            notifications.Add(new
            {
                Id = reader.GetGuid(0),
                Title = reader.GetString(1),
                Message = reader.GetString(2),
                IsRead = reader.GetBoolean(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return Ok(notifications);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await using var connection =
            new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            UPDATE public.notifications
            SET is_read = TRUE
            WHERE id = @id AND user_id = @userId;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("userId", userId.Value);

        var updated = await command.ExecuteNonQueryAsync();

        return updated > 0
            ? Ok(new { Message = "Notification marked as read." })
            : NotFound();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claim, out var id)
            ? id : null;
    }
}
