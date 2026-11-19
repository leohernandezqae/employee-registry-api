using System.Text;
using EmployeeRegistry.Api.Data;
using EmployeeRegistry.Api.Modules.Auth;
using EmployeeRegistry.Api.Modules.Employees;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

foreach (var source in builder.Configuration.Sources)
{
    if (source is FileConfigurationSource file &&
        file.Path is not null &&
        file.Path.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase))
    {
        file.Path = Path.Combine("Configuration", file.Path);
    }
}

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Set Jwt__Key to a secret of at least 32 characters.");

    jwtKey = "development-only-key-change-me-please-32b";
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");

var jwtOptions = new JwtOptions
{
    Issuer = jwtSection["Issuer"] ?? "EmployeeRegistry",
    Audience = jwtSection["Audience"] ?? "EmployeeRegistry",
    Key = jwtKey,
    ExpiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var minutes) && minutes > 0 ? minutes : 60
};

var sqlite = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=employee-registry.sqlite");
if (!Path.IsPathRooted(sqlite.DataSource))
    sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);

builder.Services.AddSingleton(Options.Create(jwtOptions));
builder.Services.AddSingleton(new PasswordHasher<Account>());
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqlite.ConnectionString));
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddControllers();
var configuredOrigins = builder.Configuration.GetSection("Web:Origins").Get<string[]>();
var webOrigins = configuredOrigins is { Length: > 0 }
    ? configuredOrigins
    : new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Web", policy => policy.WithOrigins(webOrigins).AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var message = string.IsNullOrEmpty(context.Request.Headers.Authorization)
                    ? "Token required"
                    : "Invalid or expired token";
                await context.Response.WriteAsJsonAsync(new { message });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "You do not have permission to change employees." });
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole(Roles.Admin));
});

var app = builder.Build();

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Unhandled");
            logger.LogError(feature.Error, "Unhandled exception");
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = "Unexpected error." });
    });
});

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("Web");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallback("/api/{**path}", () =>
    Results.Json(new { message = "Not found" }, statusCode: StatusCodes.Status404NotFound));

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");
    await db.Database.CloseConnectionAsync();
    SqliteConnection.ClearAllPools();
    await DbSeeder.SeedAsync(
        db,
        app.Configuration,
        app.Environment,
        scope.ServiceProvider.GetRequiredService<PasswordHasher<Account>>());
}

app.Run();

public partial class Program;
