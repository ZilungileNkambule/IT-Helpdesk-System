
using System.Net.Http.Headers;
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

[Authorize(
    AuthenticationSchemes =
        CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = "technician,manager")]
[Route("WebProfile")]
public class WebProfileController : Controller
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public WebProfileController(
        IConfiguration config,
        IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(
            ClaimTypes.NameIdentifier)!);

    private NpgsqlConnection Connection() =>
        new(_config.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException(
                "Missing Supabase connection string."));

    private async Task<ProfileViewModel?> LoadProfile()
    {
        await using var db = Connection();
        await db.OpenAsync();

        const string sql = """
            SELECT id, COALESCE(full_name, ''),
                   COALESCE(email, ''),
                   COALESCE(role, ''),
                   is_active, created_at
            FROM public.profiles
            WHERE id = @id;
            """;

        await using var cmd = new NpgsqlCommand(sql, db);
        cmd.Parameters.AddWithValue("id", CurrentUserId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new ProfileViewModel
        {
            Id = reader.GetGuid(0),
            FullName = reader.GetString(1),
            Email = reader.GetString(2),
            Role = reader.GetString(3),
            IsActive = reader.GetBoolean(4),
            CreatedAt = reader.IsDBNull(5)
                ? null
                : reader.GetDateTime(5)
        };
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var profile = await LoadProfile();

        return profile == null
            ? NotFound("Profile not found.")
            : View(profile);
    }

    [HttpPost("UpdateName")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateName(
        string fullName)
    {
        fullName = (fullName ?? "").Trim();

        if (fullName.Length is < 2 or > 150)
        {
            TempData["Error"] =
                "Full name must contain 2 to 150 characters.";

            return RedirectToAction(nameof(Index));
        }

        await using var db = Connection();
        await db.OpenAsync();

        const string sql = """
            UPDATE public.profiles
            SET full_name = @name
            WHERE id = @id AND is_active = TRUE;
            """;

        await using var cmd = new NpgsqlCommand(sql, db);
        cmd.Parameters.AddWithValue("name", fullName);
        cmd.Parameters.AddWithValue("id", CurrentUserId);

        int updated = await cmd.ExecuteNonQueryAsync();

        TempData[updated > 0 ? "Success" : "Error"] =
            updated > 0
                ? "Your name was updated successfully."
                : "Unable to update your profile.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> SignInToSupabase(
        string email,
        string password)
    {
        string url = _config["Supabase:Url"]
            ?? throw new InvalidOperationException(
                "Missing Supabase URL.");

        string key = _config["Supabase:AnonKey"]
            ?? throw new InvalidOperationException(
                "Missing Supabase anon key.");

        using var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("apikey", key);

        using var response = await client.PostAsJsonAsync(
            $"{url.TrimEnd('/')}/auth/v1/token?grant_type=password",
            new { email, password });

        if (!response.IsSuccessStatusCode)
            return null;

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var root = json.RootElement;

        // Ensure the password belongs to this exact user.
        var authenticatedId = root
            .GetProperty("user")
            .GetProperty("id")
            .GetString();

        if (!Guid.TryParse(authenticatedId, out var id) ||
            id != CurrentUserId)
        {
            return null;
        }

        return root.GetProperty("access_token").GetString();
    }

    private async Task<bool> UpdateSupabaseUser(
        string accessToken,
        object changes)
    {
        string url = _config["Supabase:Url"]!;
        string key = _config["Supabase:AnonKey"]!;

        using var client = _httpFactory.CreateClient();

        client.DefaultRequestHeaders.Add("apikey", key);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", accessToken);

        using var response = await client.PutAsJsonAsync(
            $"{url.TrimEnd('/')}/auth/v1/user",
            changes);

        return response.IsSuccessStatusCode;
    }

    [HttpPost("UpdateEmail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail(
        string newEmail,
        string currentPassword)
    {
        newEmail = (newEmail ?? "").Trim();

        if (!new System.ComponentModel.DataAnnotations
            .EmailAddressAttribute().IsValid(newEmail))
        {
            TempData["Error"] =
                "Enter a valid email address.";

            return RedirectToAction(nameof(Index));
        }

        var profile = await LoadProfile();

        if (profile == null)
            return NotFound();

        if (string.Equals(
            profile.Email, newEmail,
            StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] =
                "This is already your current email.";

            return RedirectToAction(nameof(Index));
        }

        var token = await SignInToSupabase(
            profile.Email, currentPassword ?? "");

        if (token == null)
        {
            TempData["Error"] =
                "Current password is incorrect.";

            return RedirectToAction(nameof(Index));
        }

        bool requested = await UpdateSupabaseUser(
            token,
            new { email = newEmail });

        if (!requested)
        {
            TempData["Error"] =
                "Email change could not be requested.";

            return RedirectToAction(nameof(Index));
        }

        // Do not update profiles.email yet.
        // Supabase may require email confirmation first.
        TempData["Success"] =
            "Email change requested. Check your email for " +
            "confirmation instructions. Your current email " +
            "remains displayed until the change is verified.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangeProfilePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Enter your current password and matching " +
                "new passwords (at least 8 characters).";

            return RedirectToAction(nameof(Index));
        }

        var profile = await LoadProfile();

        if (profile == null)
            return NotFound();

        var token = await SignInToSupabase(
            profile.Email, model.CurrentPassword);

        if (token == null)
        {
            TempData["Error"] =
                "Current password is incorrect.";

            return RedirectToAction(nameof(Index));
        }

        if (model.CurrentPassword == model.NewPassword)
        {
            TempData["Error"] =
                "Choose a different password.";

            return RedirectToAction(nameof(Index));
        }

        bool changed = await UpdateSupabaseUser(
            token,
            new { password = model.NewPassword });

        if (!changed)
        {
            TempData["Error"] =
                "Password update failed. Please try again.";

            return RedirectToAction(nameof(Index));
        }

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        TempData["Success"] =
            "Password changed. Please log in again.";

        return Redirect("/Account/Login");
    }
}

