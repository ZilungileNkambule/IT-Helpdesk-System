
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ITHelpdeskAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ITHelpdeskAPI.Controllers;

[Route("Account")]
public class WebAccountController : Controller
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebAccountController(
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string? supabaseUrl =
            _config["Supabase:Url"];

        string? anonKey =
            _config["Supabase:AnonKey"];

        if (string.IsNullOrWhiteSpace(supabaseUrl) ||
            string.IsNullOrWhiteSpace(anonKey))
        {
            ModelState.AddModelError(
                "",
                "Authentication is not configured.");

            return View(model);
        }

        using var client =
            _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "apikey",
            anonKey);

        HttpResponseMessage response;

        try
        {
            response = await client.PostAsJsonAsync(
                $"{supabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=password",
                new
                {
                    email = model.Email.Trim(),
                    password = model.Password
                });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                "",
                "Authentication service unavailable.");

            return View(model);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }

            using var json =
                await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync());

            var root = json.RootElement;

            string? userIdText = root
                .GetProperty("user")
                .GetProperty("id")
                .GetString();


            string verifiedEmail = root
                .GetProperty("user")
                .GetProperty("email")
                .GetString() ?? model.Email.Trim();


            if (!Guid.TryParse(userIdText, out Guid userId))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid account.");

                return View(model);
            }

            string connectionString =
                _config.GetConnectionString("Supabase")
                ?? throw new InvalidOperationException(
                    "Supabase connection string is missing.");

            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync();


            const string syncEmailSql = """
            UPDATE public.profiles
            SET email = @email
            WHERE id = @userId
            AND is_active = TRUE
            AND email IS DISTINCT FROM @email;
            """;

            await using (var syncCommand =
                new NpgsqlCommand(syncEmailSql, connection))
            {
                syncCommand.Parameters.AddWithValue(
                    "email", verifiedEmail);

                syncCommand.Parameters.AddWithValue(
                    "userId", userId);

                await syncCommand.ExecuteNonQueryAsync();
            }


            const string sql = """
                SELECT role, is_active, email
                FROM public.profiles
                WHERE id = @userId;
                """;

            await using var command =
                new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "userId",
                userId);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                ModelState.AddModelError(
                    "",
                    "Your user profile was not found.");

                return View(model);
            }

            string role = reader.IsDBNull(0)
                ? ""
                : reader.GetString(0);

            bool isActive =
                !reader.IsDBNull(1) &&
                reader.GetBoolean(1);

            string email = reader.IsDBNull(2)
                ? model.Email.Trim()
                : reader.GetString(2);

            if (!isActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has been deactivated. Contact your IT manager.");

                return View(model);
            }

            if (role is not ("technician" or "manager"))
            {
                ModelState.AddModelError(
                    "",
                    "This portal is for IT staff only.");

                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString()),

                new Claim(
                    ClaimTypes.Email,
                    email),

                new Claim(
                    ClaimTypes.Role,
                    role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

            return RedirectToAction(
                "Index",
                "Dashboard");
        }
    }

    [HttpPost("Logout")]
    [Authorize(
        AuthenticationSchemes =
            CookieAuthenticationDefaults.AuthenticationScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login");
    }

    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied()
    {
        return Content(
            "Access denied. You do not have permission to view this page.");
    }
}
