
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;

namespace ITHelpdeskAPI.Services;

public class WebCookieValidationService
{
    private readonly IConfiguration _configuration;

    public WebCookieValidationService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task ValidateAsync(
        CookieValidatePrincipalContext context)
    {
        string? userIdText = context.Principal?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdText, out Guid userId))
        {
            await RejectAsync(context);
            return;
        }

        string? connectionString =
            _configuration.GetConnectionString("Supabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await RejectAsync(context);
            return;
        }

        string role;
        bool isActive;

        try
        {
            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync(
                context.HttpContext.RequestAborted);

            const string sql = """
                SELECT role, is_active
                FROM public.profiles
                WHERE id = @userId;
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "userId",
                userId);

            await using var reader =
                await command.ExecuteReaderAsync(
                    context.HttpContext.RequestAborted);

            if (!await reader.ReadAsync(
                context.HttpContext.RequestAborted))
            {
                await RejectAsync(context);
                return;
            }

            role = reader.IsDBNull(0)
                ? ""
                : reader.GetString(0);

            isActive =
                !reader.IsDBNull(1) &&
                reader.GetBoolean(1);
        }
        catch (NpgsqlException)
        {
            // Fail closed if account status cannot be verified.
            await RejectAsync(context);
            return;
        }

        if (!isActive ||
            role is not ("technician" or "manager"))
        {
            await RejectAsync(context);
            return;
        }

        string? existingRole = context.Principal?
            .FindFirstValue(ClaimTypes.Role);

        if (string.Equals(
            existingRole,
            role,
            StringComparison.Ordinal))
        {
            return;
        }

        var identity = context.Principal?
            .Identity as ClaimsIdentity;

        if (identity == null)
        {
            await RejectAsync(context);
            return;
        }

        foreach (var claim in identity
            .FindAll(ClaimTypes.Role)
            .ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(
            new Claim(ClaimTypes.Role, role));

        context.ReplacePrincipal(
            new ClaimsPrincipal(identity));

        context.ShouldRenew = true;
    }

    private static async Task RejectAsync(
        CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}

