
using System.Security.Claims;
using ITHelpdeskAPI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// MVC, API AND SERVICES
// --------------------------------------------------

builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();

builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<WebCookieValidationService>();

builder.Services.AddHttpClient();

// --------------------------------------------------
// SUPABASE CONFIGURATION
// --------------------------------------------------

var supabaseUrl =
    builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException(
        "Supabase URL is missing.");

var issuer =
    $"{supabaseUrl.TrimEnd('/')}/auth/v1";

// --------------------------------------------------
// AUTHENTICATION
// --------------------------------------------------

builder.Services
    .AddAuthentication(options =>
    {
        // API requests use Supabase JWTs by default.
        // MVC controllers explicitly request cookie auth.
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })

    // SUPABASE JWT AUTHENTICATION
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = "authenticated",

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                NameClaimType = "sub"
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdText =
                    context.Principal?.FindFirstValue("sub")
                    ?? context.Principal?.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(
                    userIdText,
                    out Guid userId))
                {
                    context.Fail("Invalid user identity.");
                    return;
                }

                var configuration =
                    context.HttpContext.RequestServices
                        .GetRequiredService<IConfiguration>();

                var connectionString =
                    configuration.GetConnectionString("Supabase");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    context.Fail(
                        "Account verification is unavailable.");

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
                        context.Fail("User profile not found.");
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
                    context.Fail(
                        "Account verification is unavailable.");

                    return;
                }

                if (!isActive)
                {
                    context.Fail(
                        "This account has been deactivated.");

                    return;
                }

                if (role is not (
                    "staff" or "technician" or "manager"))
                {
                    context.Fail("Invalid account role.");
                    return;
                }

                // Use the current database role,
                // not a potentially outdated token role.
                if (context.Principal?.Identity
                    is ClaimsIdentity identity)
                {
                    foreach (var claim in identity
                        .FindAll(ClaimTypes.Role)
                        .ToList())
                    {
                        identity.RemoveClaim(claim);
                    }

                    identity.AddClaim(
                        new Claim(ClaimTypes.Role, role));
                }
                else
                {
                    context.Fail("Invalid identity.");
                }
            }
        };
    })

    // MVC COOKIE AUTHENTICATION
    .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.LoginPath = "/Account/Login";

            options.AccessDeniedPath =
                "/Account/AccessDenied";

            options.Cookie.HttpOnly = true;

            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;

            options.Cookie.SameSite =
                SameSiteMode.Lax;

            options.Events.OnValidatePrincipal =
                async context =>
                {
                    var validator =
                        context.HttpContext.RequestServices
                            .GetRequiredService<
                                WebCookieValidationService>();

                    await validator.ValidateAsync(context);
                };
        });

// --------------------------------------------------
// AUTHORIZATION
// --------------------------------------------------

builder.Services.AddAuthorization();

// --------------------------------------------------
// BUILD APPLICATION
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// HTTP PIPELINE
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

// Attribute-routed API and MVC controllers.
app.MapControllers();

// Conventional MVC routes.
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
