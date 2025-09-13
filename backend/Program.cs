
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SocialApp.Api.Auth;
using SocialApp.Api.Data;
using SocialApp.Api.Models.Core;
using SocialApp.Api.Models.Auth;
using SocialApp.Api.Models.Social;
using SocialApp.Api.Models.Notify;
//using SocialApp.Api.Models;


var builder = WebApplication.CreateBuilder(args);

// EF
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
const string CorsPolicy = "_cors";
builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p => p
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions
{
    Issuer = "Dev", Audience = "DevClient", Secret = "CHANGE_ME"
};
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            IssuerSigningKey = key,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization();

// Services
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<TokenService>();

var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// === Friends (受保護) ===
// 取 JWT 的使用者 Id（sub）
// static long? GetUserId(HttpContext ctx)
// {
//     var sub = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
//            ?? ctx.User.FindFirst("sub")?.Value;
//     return long.TryParse(sub, out var id) ? id : null;
// }


// --- 範例 API ---
var summaries = new[] { "Freezing","Bracing","Chilly","Cool","Mild","Warm","Balmy","Hot","Sweltering","Scorching" };

app.MapGet("/db-ping", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return Results.Ok(new { ok });
});

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(i =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();
    return forecast;
});


// === Auth Endpoints ===
app.MapPost("/auth/register", async (RegisterRequest req, AppDbContext db, PasswordHasher hasher) =>
{
    // --- 基本檢核 ---
    if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3)
        return Results.BadRequest(new { message = "Username too short" });
    if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
        return Results.BadRequest(new { message = "Password too short" });
    if (string.IsNullOrWhiteSpace(req.UserId))
        return Results.BadRequest(new { message = "UserId required" });
    if (string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { message = "Email required" });

    // 正規化（小寫、去空白）
    var userId = req.UserId.Trim().ToLowerInvariant();
    var email  = req.Email.Trim().ToLowerInvariant();
    var username = req.Username.Trim();

    // 規則：UserId 僅允許 a-z 0-9 . _ -，長度 3~32
    if (userId.Length < 3 || userId.Length > 32 || !System.Text.RegularExpressions.Regex.IsMatch(userId, "^[a-z0-9._-]+$"))
        return Results.BadRequest(new { message = "Invalid UserId (a-z,0-9,._-, length 3–32)" });

    // --- 唯一性檢查 ---
    if (await db.Users.AnyAsync(u => u.Username == username))
        return Results.Conflict(new { code = "USERNAME_EXISTS", message = "Username taken" });
    if (await db.Users.AnyAsync(u => u.UserId == userId))
        return Results.Conflict(new { code = "USERID_EXISTS", message = "UserId taken" });
    if (await db.Users.AnyAsync(u => u.Email == email))
        return Results.Conflict(new { code = "EMAIL_EXISTS", message = "Email taken" });

    // --- 建立使用者 ---
    var u = new User
    {
        Username = username,
        UserId = userId,
        Email = email,
        PasswordHash = hasher.Hash(req.Password)
    };

    db.Users.Add(u);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Registered" });
});


// /auth/login
app.MapPost("/auth/login", async (LoginRequest req, AppDbContext db, PasswordHasher hasher, TokenService tokens, HttpResponse resp) =>
{
    var name = req.Username?.Trim();                     // 仍用 Username 登入
    var u = await db.Users.FirstOrDefaultAsync(x => x.Username == name);
    if (u is null) return Results.Unauthorized();
    if (!hasher.Verify(req.Password, u.PasswordHash)) return Results.Unauthorized();

    var access = tokens.IssueAccessToken(u);
    await tokens.IssueRefreshAsync(u, resp);             // HttpOnly refresh cookie

    var userDto = new { id = u.Id, username = u.Username, userId = u.UserId, email = u.Email };
    return Results.Ok(new AuthResponse(access, userDto));
});

// /auth/refresh
app.MapPost("/auth/refresh", async (TokenService tokens, HttpRequest req) =>
{
    var u = await tokens.ValidateRefreshAsync(req);
    if (u is null) return Results.Unauthorized();

    var access = tokens.IssueAccessToken(u);
    var userDto = new { id = u.Id, username = u.Username, userId = u.UserId, email = u.Email };
    return Results.Ok(new AuthResponse(access, userDto));
});

app.MapPost("/auth/logout", async (TokenService tokens, HttpRequest req, HttpResponse resp) =>
{
    await tokens.RevokeRefreshAsync(req, resp);
    return Results.Ok(new { message = "Logged out" });
});

app.Run();

// === Auth DTO ===
public record RegisterRequest(string Username, string Password, string UserId, string Email);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string AccessToken, object User);

// === record 型別必須放在最後 ===
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 9.0 / 5.0 + 32);
}

