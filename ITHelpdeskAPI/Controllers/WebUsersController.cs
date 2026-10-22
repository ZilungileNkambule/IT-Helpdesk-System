
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ITHelpdeskAPI.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ITHelpdeskAPI.Controllers;

[Authorize(
    AuthenticationSchemes =
        CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "manager")]
[Route("WebUsers")]
public class WebUsersController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebUsersController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    private NpgsqlConnection CreateConnection()
    {
        string connectionString =
            _configuration.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException(
                "Supabase connection string is missing.");

        return new NpgsqlConnection(connectionString);
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var model = new UserManagementViewModel();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            SELECT
                id,
                COALESCE(full_name, ''),
                COALESCE(email, ''),
                COALESCE(role, 'staff'),
                is_active
            FROM public.profiles
            ORDER BY full_name, email;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            model.Users.Add(new ManagedUser
            {
                Id = reader.GetGuid(0),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),
                Role = reader.GetString(3),
                IsActive = reader.GetBoolean(4)
            });
        }

        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateManagedUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            !IsValidRole(request.Role))
        {
            TempData["Error"] = "Enter valid user details.";
            return RedirectToAction(nameof(Index));
        }

        if (request.Password.Length < 8)
        {
            TempData["Error"] =
                "Password must contain at least 8 characters.";

            return RedirectToAction(nameof(Index));
        }

        string supabaseUrl =
            _configuration["Supabase:Url"]
            ?? throw new InvalidOperationException(
                "Supabase URL is missing.");

        string serviceRoleKey =
            _configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException(
                "Supabase service role key is missing.");

        using var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                serviceRoleKey);

        client.DefaultRequestHeaders.Add(
            "apikey",
            serviceRoleKey);

        var payload = new
        {
            email = request.Email.Trim(),
            password = request.Password,
            email_confirm = true,
            user_metadata = new
            {
                full_name = request.FullName.Trim()
            }
        };

        var response = await client.PostAsJsonAsync(
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/admin/users",
            payload);

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] =
                "Could not create user. Check whether the email already exists.";

            return RedirectToAction(nameof(Index));
        }

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Guid userId = document.RootElement
            .GetProperty("id")
            .GetGuid();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            INSERT INTO public.profiles
                (id, full_name, email, role, is_active)
            VALUES
                (@id, @name, @email, @role, TRUE)
            ON CONFLICT (id)
            DO UPDATE SET
                full_name = EXCLUDED.full_name,
                email = EXCLUDED.email,
                role = EXCLUDED.role,
                is_active = TRUE;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", userId);
        command.Parameters.AddWithValue(
            "name", request.FullName.Trim());
        command.Parameters.AddWithValue(
            "email", request.Email.Trim());
        command.Parameters.AddWithValue(
            "role", request.Role.ToLowerInvariant());

        await command.ExecuteNonQueryAsync();

        TempData["Success"] = "User created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("UpdateRole")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(
        Guid userId,
        string role)
    {
        if (!IsValidRole(role))
        {
            TempData["Error"] = "Invalid role.";
            return RedirectToAction(nameof(Index));
        }

        var currentUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId.ToString().Equals(
            currentUserId,
            StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] =
                "You cannot change your own role.";

            return RedirectToAction(nameof(Index));
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE public.profiles
            SET role = @role
            WHERE id = @id;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "role", role.ToLowerInvariant());

        command.Parameters.AddWithValue("id", userId);

        int affected = await command.ExecuteNonQueryAsync();

        TempData[affected > 0 ? "Success" : "Error"] =
            affected > 0
                ? "User role updated."
                : "User not found.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid userId)
    {
        var currentUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId.ToString().Equals(
            currentUserId,
            StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] =
                "You cannot deactivate your own account.";

            return RedirectToAction(nameof(Index));
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        const string sql = """
            UPDATE public.profiles
            SET is_active = NOT is_active
            WHERE id = @id
            RETURNING is_active;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", userId);

        var result = await command.ExecuteScalarAsync();

        if (result is bool isActive)
        {
            TempData["Success"] = isActive
                ? "User account reactivated."
                : "User account deactivated.";
        }
        else
        {
            TempData["Error"] = "User not found.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool IsValidRole(string? role)
    {
        return role is "staff" or "technician" or "manager";
    }
}
