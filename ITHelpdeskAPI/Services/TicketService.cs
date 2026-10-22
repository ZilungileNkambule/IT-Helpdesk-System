
using ITHelpdeskAPI.DTOs;
using ITHelpdeskAPI.Models;
using Npgsql;

namespace ITHelpdeskAPI.Services
{
    public class TicketService
    {
        private readonly string _connectionString;

        public TicketService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("Supabase")
                ?? throw new InvalidOperationException(
                    "Supabase connection string is missing.");
        }

        // GET: Retrieve tickets
        public async Task<List<Ticket>> GetTicketsAsync(
            Guid userId,
            string role)
        {
            var tickets = new List<Ticket>();

            await using var connection =
                new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT
                    id,
                    reference_number,
                    title,
                    description,
                    category_id,
                    priority,
                    status,
                    created_by,
                    assigned_to,
                    resolution_notes,
                    created_at,
                    updated_at,
                    resolved_at,
                    closed_at
                FROM public.tickets
                WHERE created_by = @userId
                   OR @role IN ('technician', 'manager')
                ORDER BY created_at DESC;
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("role", role);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tickets.Add(new Ticket
                {
                    Id = reader.GetGuid(0),
                    ReferenceNumber = reader.IsDBNull(1)
                        ? null : reader.GetString(1),
                    Title = reader.GetString(2),
                    Description = reader.GetString(3),
                    CategoryId = reader.GetInt64(4),
                    Priority = reader.GetString(5),
                    Status = reader.GetString(6),
                    CreatedBy = reader.GetGuid(7),
                    AssignedTo = reader.IsDBNull(8)
                        ? null : reader.GetGuid(8),
                    ResolutionNotes = reader.IsDBNull(9)
                        ? null : reader.GetString(9),
                    CreatedAt = reader.GetDateTime(10),
                    UpdatedAt = reader.GetDateTime(11),
                    ResolvedAt = reader.IsDBNull(12)
                        ? null : reader.GetDateTime(12),
                    ClosedAt = reader.IsDBNull(13)
                        ? null : reader.GetDateTime(13)
                });
            }

            return tickets;
        }

        // POST: Create a new ticket
        public async Task<Ticket> CreateTicketAsync(
            CreateTicketDto dto,
            Guid userId)
        {
            await using var connection =
                new NpgsqlConnection(_connectionString);

            await connection.OpenAsync();

            const string sql = """
                INSERT INTO public.tickets
                    (title, description, category_id,
                     priority, created_by)
                VALUES
                    (@title, @description, @categoryId,
                     @priority, @userId)
                RETURNING
                    id, reference_number, title,
                    description, category_id, priority,
                    status, created_by, created_at,
                    updated_at;
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("title", dto.Title);
            command.Parameters.AddWithValue(
                "description", dto.Description);
            command.Parameters.AddWithValue(
                "categoryId", dto.CategoryId!.Value);
            command.Parameters.AddWithValue(
                "priority", dto.Priority);
            command.Parameters.AddWithValue("userId", userId);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "Ticket could not be created.");
            }

            return new Ticket
            {
                Id = reader.GetGuid(0),
                ReferenceNumber = reader.GetString(1),
                Title = reader.GetString(2),
                Description = reader.GetString(3),
                CategoryId = reader.GetInt64(4),
                Priority = reader.GetString(5),
                Status = reader.GetString(6),
                CreatedBy = reader.GetGuid(7),
                CreatedAt = reader.GetDateTime(8),
                UpdatedAt = reader.GetDateTime(9)
            };
        }


        public async Task<string?> GetRoleAsync(Guid userId)
        {
            await using var connection =
                new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
        SELECT role FROM public.profiles
        WHERE id = @userId;
        """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("userId", userId);

            return await command.ExecuteScalarAsync() as string;
        }

        public async Task<bool> AssignTicketAsync(
            Guid ticketId, Guid technicianId)
        {
            await using var connection =
                new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
        UPDATE public.tickets
        SET assigned_to = @technicianId
        WHERE id = @ticketId
          AND EXISTS (
              SELECT 1 FROM public.profiles
              WHERE id = @technicianId
                AND role = 'technician'
          );
        """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("ticketId", ticketId);
            command.Parameters.AddWithValue(
                "technicianId", technicianId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> SelfAssignTicketAsync(
            Guid ticketId, Guid technicianId)
        {
            await using var connection =
                new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
        UPDATE public.tickets
        SET assigned_to = @technicianId
        WHERE id = @ticketId
          AND assigned_to IS NULL
          AND status = 'Open';
        """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("ticketId", ticketId);
            command.Parameters.AddWithValue(
                "technicianId", technicianId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateTicketStatusAsync(
            Guid ticketId,
            string status,
            string? resolutionNotes)
        {
            await using var connection =
                new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = """
        UPDATE public.tickets
        SET status = @status,
            resolution_notes = COALESCE(
                @notes, resolution_notes),
            resolved_at = CASE
                WHEN @status = 'Resolved' THEN NOW()
                WHEN @status = 'Open' THEN NULL
                ELSE resolved_at
            END,
            closed_at = CASE
                WHEN @status = 'Closed' THEN NOW()
                WHEN @status = 'Open' THEN NULL
                ELSE closed_at
            END
        WHERE id = @ticketId;
        """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("ticketId", ticketId);
            command.Parameters.AddWithValue("status", status);
            command.Parameters.AddWithValue(
                "notes", (object?)resolutionNotes ?? DBNull.Value);

            return await command.ExecuteNonQueryAsync() > 0;
        }

    }
}

