
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;

namespace ITHelpdeskAPI.Controllers;

public class AddCommentDto
{
    public string Content { get; set; } = string.Empty;
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly string _connectionString;

    public CommentsController(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException(
                "Database connection missing.");
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetComments(Guid ticketId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await using var connection =
            new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            SELECT c.id, c.ticket_id, c.user_id,
                   c.content, c.created_at
            FROM public.comments c
            JOIN public.tickets t ON t.id = c.ticket_id
            WHERE c.ticket_id = @ticketId
              AND (
                  t.created_by = @userId
                  OR EXISTS (
                      SELECT 1 FROM public.profiles p
                      WHERE p.id = @userId
                        AND p.role IN ('technician', 'manager')
                  )
              )
            ORDER BY c.created_at;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("ticketId", ticketId);
        command.Parameters.AddWithValue("userId", userId.Value);

        var comments = new List<object>();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            comments.Add(new
            {
                Id = reader.GetGuid(0),
                TicketId = reader.GetGuid(1),
                UserId = reader.GetGuid(2),
                Content = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return Ok(comments);
    }

    [HttpPost("{ticketId:guid}")]
    public async Task<IActionResult> AddComment(
        Guid ticketId, AddCommentDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Comment cannot be empty.");

        await using var connection =
            new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO public.comments
                (ticket_id, user_id, content)
            SELECT t.id, @userId, @content
            FROM public.tickets t
            WHERE t.id = @ticketId
              AND (
                  t.created_by = @userId
                  OR EXISTS (
                      SELECT 1 FROM public.profiles p
                      WHERE p.id = @userId
                        AND p.role IN ('technician', 'manager')
                  )
              )
            RETURNING id;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("ticketId", ticketId);
        command.Parameters.AddWithValue("userId", userId.Value);
        command.Parameters.AddWithValue(
            "content", dto.Content.Trim());

        var result = await command.ExecuteScalarAsync();

        if (result == null)
            return NotFound("Ticket unavailable.");

        return Ok(new
        {
            Message = "Comment added successfully.",
            CommentId = result
        });
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
