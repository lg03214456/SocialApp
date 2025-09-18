
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
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://26.165.84.169:5173")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions
{
    Issuer = "Dev",
    Audience = "DevClient",
    Secret = "CHANGE_ME"
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
static long? GetUserId(HttpContext ctx)
{
    var sub = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? ctx.User.FindFirst("sub")?.Value;
    return long.TryParse(sub, out var id) ? id : null;
}

// 👇👇👇 這一塊貼在這裡
// === Helpers / DTO（供 friend-requests 使用）===
static string ToUiStatus(string s)
    => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

// static bool IsValidReqStatus(string? s)
//     => s is "pending" or "accepted" or "rejected" or "canceled";


// 👆👆👆 到這裡



// --- 範例 API ---
var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

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
    var email = req.Email.Trim().ToLowerInvariant();
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


// 依關鍵字完全比對使用者（UserId / Username / Email），排除自己
app.MapGet("/users/search", async (HttpContext ctx, AppDbContext db, string? q) =>
{
    // 要登入
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();

    // 參數整理
    var key = q?.Trim();
    if (string.IsNullOrEmpty(key)) return Results.Ok(Array.Empty<object>());

    var hasAt = key.Contains('@');
    var keyLower = key.ToLowerInvariant(); // 用於 userId/email 的規範比較（你的資料已存小寫）

    // 完全比對的查詢：
    // - userId：用小寫完全相等
    // - username：原樣完全相等
    // - email：只有當輸入包含 @ 才檢查，並用小寫完全相等
    var list = await db.Users
        .Where(u => u.Id != selfId &&
               (
                   u.UserId == keyLower ||
                   u.Username == key ||
                   (hasAt && u.Email != null && u.Email == keyLower)
               ))
        .OrderBy(u => u.UserId)
        .Select(u => new
        {
            id = u.Id,
            userId = u.UserId,
            username = u.Username,
            avatar = (string?)null
        })
        .ToListAsync();

    return Results.Ok(list);
})
.RequireAuthorization();




// 送出好友邀請
app.MapPost("/friend/friend-requests", async (HttpContext ctx, AppDbContext db, CreateFriendReq req) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();

    var toUserId = req.ToUserId?.Trim().ToLowerInvariant();
    if (string.IsNullOrEmpty(toUserId))
        return Results.BadRequest(new { message = "toUserId required" });

    // 找受邀者（userId 完全相等；資料已小寫化）
    var toUser = await db.Users.FirstOrDefaultAsync(u => u.UserId == toUserId);
    if (toUser is null) return Results.NotFound(new { message = "User not found" });
    if (toUser.Id == selfId) return Results.BadRequest(new { message = "Cannot invite yourself" });

    // 是否已有「雙向」待處理邀請？
    var hasPendingEither = await db.FriendRequests.AnyAsync(fr =>
        fr.Status == "pending" &&
        (
            (fr.FromUserId == selfId && fr.ToUserId == toUser.Id) ||
            (fr.FromUserId == toUser.Id && fr.ToUserId == selfId)
        ));
    if (hasPendingEither)
        return Results.Conflict(new { message = "Pending request already exists" });

    var now = DateTimeOffset.UtcNow;
    var entity = new SocialApp.Api.Models.Social.FriendRequest
    {

        FromUserId = selfId.Value,
        ToUserId = toUser.Id,
        Message = string.IsNullOrWhiteSpace(req.Message) ? null : req.Message!.Trim(),
        Status = "pending",
        CreatedAt = now,
        UpdatedAt = now
    };
    db.FriendRequests.Add(entity);
    await db.SaveChangesAsync();

    // 取自己 userId（字串）
    var fromUserId = await db.Users.Where(u => u.Id == selfId).Select(u => u.UserId).FirstAsync();

    // 回傳 shape 統一：createdAt 用 ISO 字串
    var dto = new
    {
        requestId = entity.RequestId,
        fromUserId,
        toUserId = toUser.UserId,
        status = ToUiStatus(entity.Status),
        message = entity.Message,
        createdAt = entity.CreatedAt.ToString("o")
    };
    return Results.Created($"/friend/friend-requests/{entity.RequestId}", dto);
}).RequireAuthorization();


// 取邀請清單
// GET /friend/friend-requests?box=incoming|outgoing&status=pending&take=20
app.MapGet("/friend/friend-requests", async (
    HttpContext ctx,
    AppDbContext db,
    int? take) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();

    var limit = (take is > 0 and <= 100) ? take!.Value : 20;

    var items = await db.FriendRequests
        .AsNoTracking()
        .Where(r => r.Status == "pending" && (r.FromUserId == selfId || r.ToUserId == selfId))
        .OrderByDescending(r => r.CreatedAt)
        .Take(limit)
        .Select(r => new
        {
            requestId = r.RequestId,   // PK
            fromUserId = r.FromUser.UserId,  // 直接回 Id 就好
            toUserId = r.ToUser.UserId,
            status = "Pending",     // 這支固定 pending
            message = r.Message,
            createdAt = r.CreatedAt.ToString("o")
        })
        .ToListAsync();

    return Results.Ok(items);
}).RequireAuthorization();


// 接受邀請
app.MapPost("/friend/friend-requests/{id:long}/accept", async (HttpContext ctx, AppDbContext db, long id) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();

    var reqEntity = await db.FriendRequests.FirstOrDefaultAsync(r => r.RequestId == id);
    if (reqEntity is null) return Results.NotFound();
    if (reqEntity.ToUserId != selfId) return Results.Forbid();
    if (reqEntity.Status != "pending") return Results.Conflict(new { message = "Request already processed" });

    reqEntity.Status = "accepted";
    reqEntity.RespondedAt = DateTimeOffset.UtcNow;
    reqEntity.UpdatedAt = reqEntity.RespondedAt.Value;

    // 建立雙向好友關係（若不存在）
    async Task EnsureFriendship(long a, long b)
    {
        var exists = await db.Friendships.AnyAsync(f => f.UserId == a && f.FriendUserId == b);
        if (!exists)
        {
            db.Friendships.Add(new Friendship
            {
                UserId = a,
                FriendUserId = b,
                Status = "accepted",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }
    await EnsureFriendship(reqEntity.FromUserId, reqEntity.ToUserId);
    await EnsureFriendship(reqEntity.ToUserId, reqEntity.FromUserId);

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Accepted" });
}).RequireAuthorization();

// 拒絕邀請
app.MapPost("/friend/friend-requests/{id:long}/reject", async (HttpContext ctx, AppDbContext db, long id) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();

    var reqEntity = await db.FriendRequests.FirstOrDefaultAsync(r => r.RequestId == id);
    if (reqEntity is null) return Results.NotFound();
    if (reqEntity.ToUserId != selfId) return Results.Forbid();
    if (reqEntity.Status != "pending") return Results.Conflict(new { message = "Request already processed" });

    reqEntity.Status = "rejected";
    reqEntity.RespondedAt = DateTimeOffset.UtcNow;
    reqEntity.UpdatedAt = reqEntity.RespondedAt.Value;

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Rejected" });
}).RequireAuthorization();

// 取消我送出的邀請
app.MapPost("/friend/friend-requests/{id:long}", async (HttpContext ctx, AppDbContext db, long id) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();

    var reqEntity = await db.FriendRequests.FirstOrDefaultAsync(r => r.RequestId == id);
    if (reqEntity is null) return Results.NotFound();
    if (reqEntity.FromUserId != selfId) return Results.Forbid();
    if (reqEntity.Status != "pending") return Results.Conflict(new { message = "Request already processed" });

    reqEntity.Status = "canceled";
    reqEntity.RespondedAt = DateTimeOffset.UtcNow;
    reqEntity.UpdatedAt = reqEntity.RespondedAt.Value;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// 好友清單
app.MapGet("/friend/friends", async (HttpContext ctx, AppDbContext db, int? take) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();

    var limit = take is > 0 and <= 100 ? take!.Value : 50;

    var items = await db.Friendships
        .Where(f => f.UserId == selfId && f.Status == "accepted")
        .OrderByDescending(f => f.CreatedAt)
        .Take(limit)
        .Select(f => new {
            id = f.FriendUserId,
            userId = f.FriendUser.UserId,   // ← navigation property
            username = f.FriendUser.Username,
            createdAt = f.CreatedAt.ToString("o")
        })
        .ToListAsync();

    return Results.Ok(items);
}).RequireAuthorization();



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

public record CreateFriendReq(string ToUserId, string? Message);