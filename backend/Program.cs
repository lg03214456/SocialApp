
using Microsoft.AspNetCore.HttpOverrides; // + 新增
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SocialApp.Api.Auth;
using SocialApp.Api.Models.Auth.Dtos;
using SocialApp.Api.Data;
using SocialApp.Api.Models.Core;
//using SocialApp.Api.Models.Auth;
using SocialApp.Api.Models.Social;
//using SocialApp.Api.Models.Notify;
using SocialApp.Api.Models.Chat;
using SocialApp.Api.Models.Chat.Dtos;
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
var origins = builder.Configuration.GetSection("Cors:Origins")
                                   .Get<string[]>() ?? Array.Empty<string>();
// builder.Services.AddCors(o =>
// {
//     o.AddPolicy(CorsPolicy, p => p
//         .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://26.165.84.169:5173")
//         .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
// });
builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p => p
        .WithOrigins(origins)          // ← 改這行，不要寫死網址
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
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
// (建議) 代理/IIS 後面要拿對的 IP/HTTPS（放越前面越好）
app.UseForwardedHeaders(new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// 只在開發顯示 Swagger，正式開 HSTS/HTTPS
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// app.UseSwagger();
// app.UseSwaggerUI();

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
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();
    var me = selfId.Value;

    var key = q?.Trim();
    if (string.IsNullOrEmpty(key)) return Results.Ok(Array.Empty<object>());

    var hasAt = key.Contains('@');
    var keyLower = key.ToLowerInvariant();

    var list = await db.Users.AsNoTracking()
        .Where(u => u.Id != me &&
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
            avatar = (string?)null,

            // 只判斷是否為好友（雙向任一筆 accepted 即視為好友）
            alreadyFriend = db.Friendships.Any(f =>
                f.Status == "accepted" && (
                    (f.UserId == me && f.FriendUserId == u.Id) ||
                    (f.UserId == u.Id && f.FriendUserId == me)
                ))
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

app.MapPost("/friend/friend-requests/{id:long}/accept", async (HttpContext ctx, AppDbContext db, long id) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();
    var me = selfId.Value;

    // 取請求；只有被邀請者可接受
    var reqEntity = await db.FriendRequests.FirstOrDefaultAsync(r => r.RequestId == id);
    if (reqEntity is null) return Results.NotFound();
    if (reqEntity.ToUserId != me) return Results.Forbid();
    if (reqEntity.Status != "pending") return Results.Conflict(new { message = "Request already processed" });

    // 用交易把：變更請求狀態、建立雙向好友、建立/取得 DM → 全部原子化
    await using var tx = await db.Database.BeginTransactionAsync();

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

    // ★★★ 這裡：確保雙方有一個 DM 會話（若沒有就建立）
    var peerId = reqEntity.FromUserId;
    var conversationId = await EnsureDmConversationAsync(db, me, peerId);

    // 一次存檔 + 交易提交
    await db.SaveChangesAsync();
    await tx.CommitAsync();

    return Results.Ok(new { message = "Accepted", conversationId });
})
.RequireAuthorization();

//
// 依你現有結構的 EnsureDm：
// - 先查是否已存在 Type='dm'、且兩人都在的會話
// - 若無，建立 Conversations + 兩筆 ConversationParticipants
// - 回傳 conversationId
//
static async Task<long> EnsureDmConversationAsync(
    AppDbContext db, long user1, long user2, CancellationToken ct = default)
{
    if (user1 == user2)
        throw new InvalidOperationException("不能與自己建立 DM。");

    var a = Math.Min(user1, user2);
    var b = Math.Max(user1, user2);
    var now = DateTimeOffset.UtcNow;

    // 先找既有 DM（符合你的唯一索引：Type='dm' + A/B）
    var conv = await db.Conversations
        .FirstOrDefaultAsync(c => c.Type == "dm" && c.DmUserAId == a && c.DmUserBId == b, ct);

    if (conv is null)
    {
        // 嘗試建立；若併發撞唯一鍵，撈回既有那一筆
        conv = new Conversation
        {
            Type = "dm",
            DmUserAId = a,
            DmUserBId = b,
            Visibility = "private",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Conversations.Add(conv);
        try
        {
            await db.SaveChangesAsync(ct); // ← 回填 ConversationId（IDENTITY）
        }
        catch (DbUpdateException)
        {
            conv = await db.Conversations
                .FirstAsync(c => c.Type == "dm" && c.DmUserAId == a && c.DmUserBId == b, ct);
        }
    }
    else
    {
        // 已存在：順手刷新 UpdatedAt（可選）
        conv.UpdatedAt = now;
    }

    // 補齊/建立 participant（你的模型需要 JoinedAt 等欄位）
    async Task EnsureParticipantAsync(long userId)
    {
        var exists = await db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conv.ConversationId && cp.UserId == userId, ct);

        if (!exists)
        {
            db.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conv.ConversationId,
                UserId = userId,
                Status = "member",            // 你的預設
                Role = "member",            // 你的預設
                LastReadAt = null,
                ReadUpToMessageId = null,
                IsPinned = false,
                MuteUntil = null,
                InvitedByUserId = null,
                JoinedAt = now                  // ★ 必填，因為非 nullable
            });
        }
        else
        {
            // 若已存在但狀態不是 member（防呆；看產品需求可移除）
            var cp = await db.ConversationParticipants
                .FirstAsync(x => x.ConversationId == conv.ConversationId && x.UserId == userId, ct);

            if (cp.Status != "member") cp.Status = "member";
            if (cp.Role != "member") cp.Role = "member";
            if (cp.JoinedAt == default) cp.JoinedAt = now;
        }
    }

    await EnsureParticipantAsync(user1);
    await EnsureParticipantAsync(user2);

    // 由外層交易/SaveChanges 提交；這裡只回 ID
    return conv.ConversationId;
}




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
        .Select(f => new
        {
            id = f.FriendUserId,
            userId = f.FriendUser.UserId,   // ← navigation property
            username = f.FriendUser.Username,
            createdAt = f.CreatedAt.ToString("o")
        })
        .ToListAsync();

    return Results.Ok(items);
}).RequireAuthorization();


// 取「好友清單 + DM 會話資訊」
// GET /chat/friends-inbox?take=100

app.MapGet("/chat/friends-inbox", async (HttpContext ctx, AppDbContext db, int? take) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();
    var me = selfId.Value;

    var limit = (take is > 0 and <= 500) ? take.Value : 100;

    // 1) 我的好友（我→對方，accepted）
    var baseRows = await (
        from f in db.Friendships.AsNoTracking()
        join u in db.Users.AsNoTracking() on f.FriendUserId equals u.Id
        where f.UserId == me && f.Status == "accepted"
        select new
        {
            FriendId   = u.Id,
            FriendUid  = u.UserId ?? string.Empty,
            FriendName = u.Username ?? string.Empty
        })
        .OrderBy(x => x.FriendName)
        .Take(limit)
        .ToListAsync();

    if (baseRows.Count == 0)
        return Results.Ok(Array.Empty<FriendInboxRowDto>());

    var friendIdSet = new HashSet<long>(baseRows.Select(x => x.FriendId));

    // 2) 找出我與好友的 DM 會話（從 Conversations 為主）
    var pairs = await (
        from c in db.Conversations.AsNoTracking()
        join cpMe in db.ConversationParticipants.AsNoTracking()
            on c.ConversationId equals cpMe.ConversationId
        where c.Type == "dm"
              && c.DmUserAId != null && c.DmUserBId != null
              && cpMe.UserId == me
        let peerId = (c.DmUserAId == me ? c.DmUserBId : c.DmUserAId)
        where peerId.HasValue && friendIdSet.Contains(peerId.Value)
        select new
        {
            FriendId = peerId!.Value,
            c.ConversationId,
            cpMe.ReadUpToMessageId,
            c.CreatedAt,
            c.UpdatedAt
        })
        .ToListAsync();

    var convIdSet = new HashSet<long>(pairs.Select(p => p.ConversationId));

    // 3) 每個會話最後一則訊息（若沒有訊息 → null）
    Dictionary<long, LastMessageDto?> lastMsgMap;
    if (convIdSet.Count == 0)
    {
        lastMsgMap = new Dictionary<long, LastMessageDto?>();
    }
    else
    {
        var lastMsgs = await (
            from c in db.Conversations.AsNoTracking()
            where convIdSet.Contains(c.ConversationId)
            join m in db.Messages.AsNoTracking()
                on c.ConversationId equals m.ConversationId into gm
            from lm in gm
                .Where(x => x.DeletedAt == null)
                .OrderByDescending(x => x.MessageSeq)   // 若無此欄位可移除
                .ThenByDescending(x => x.MessageId)
                .Take(1)
                .DefaultIfEmpty()
            select new
            {
                c.ConversationId,
                Dto = (LastMessageDto?) (lm == null ? null : new LastMessageDto
                {
                    Id   = lm.MessageId,
                    Kind = lm.Kind,
                    Text = lm.Text,
                    Time = lm.CreatedAt,
                    Mine = (lm.SenderId == me)
                })
            })
            .ToListAsync();

        // 👇 這裡確保 value 為可空，避免 CS8619
        lastMsgMap = lastMsgs.ToDictionary(x => x.ConversationId, x => (LastMessageDto?)x.Dto);
    }

    // 4) 未讀數（對方發的 + 未刪除 + Id > ReadUpTo）
    Dictionary<long, int> unreadMap;
    if (convIdSet.Count == 0)
    {
        unreadMap = new Dictionary<long, int>();
    }
    else
    {
        var unreadCounts = await (
            from m in db.Messages.AsNoTracking()
            join cp in db.ConversationParticipants.AsNoTracking()
                 on m.ConversationId equals cp.ConversationId
            where convIdSet.Contains(m.ConversationId)
                  && cp.UserId == me
                  && m.SenderId != me
                  && m.DeletedAt == null
                  && (cp.ReadUpToMessageId == null || m.MessageId > cp.ReadUpToMessageId)
            group m by m.ConversationId into g
            select new { ConversationId = g.Key, Count = g.Count() }
        ).ToListAsync();

        unreadMap = unreadCounts.ToDictionary(x => x.ConversationId, x => x.Count);
    }

    var pairMap = pairs.ToDictionary(p => p.FriendId, p => p);

    // 5) 合併輸出
    var result = baseRows
        .Select(row =>
        {
            if (pairMap.TryGetValue(row.FriendId, out var p))
            {
                lastMsgMap.TryGetValue(p.ConversationId, out var last);

                return new FriendInboxRowDto
                {
                    FriendId = row.FriendId,
                    FriendUid = row.FriendUid,
                    FriendName = row.FriendName,
                    ConversationId = p.ConversationId,
                    LastMessage = last,
                    ReadUpToMessageId = p.ReadUpToMessageId,
                    UnreadCount = unreadMap.TryGetValue(p.ConversationId, out var uc) ? uc : 0
                };
            }

            // 尚未開過 DM（沒有 Conversation）
            return new FriendInboxRowDto
            {
                FriendId = row.FriendId,
                FriendUid = row.FriendUid,
                FriendName = row.FriendName,
                ConversationId = null,
                LastMessage = null,
                ReadUpToMessageId = null,
                UnreadCount = 0
            };
        })
        // 依最後訊息時間 desc（無訊息者最末），再依姓名
        .OrderByDescending(x => x.LastMessage?.Time ?? DateTimeOffset.MinValue)
        .ThenBy(x => x.FriendName)
        // 可保留或移除（前面朋友已 Take(limit)）
        .Take(limit)
        .ToList();

    return Results.Ok(result);
})
.RequireAuthorization();


app.MapPost("/chat/conversations/{id:long}/read-up-to",
async (HttpContext ctx, AppDbContext db, long id, ReadUpToRequest body) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();
    var me = selfId.Value;

    if (body is null || body.messageId <= 0)
        return Results.BadRequest(new { message = "messageId required" });

    // 我是否在這個會話內？
    var cp = await db.ConversationParticipants
        .FirstOrDefaultAsync(x => x.ConversationId == id && x.UserId == me && x.Status == "member");
    if (cp is null) return Results.Forbid();

    // 指定訊息是否存在且屬於此會話（且未軟刪）？
    var exists = await db.Messages.AsNoTracking().AnyAsync(m =>
        m.ConversationId == id && m.MessageId == body.messageId && m.DeletedAt == null);
    if (!exists) return Results.NotFound(new { message = "Message not found in this conversation" });

    // 只往前更新
    if (cp.ReadUpToMessageId == null || body.messageId > cp.ReadUpToMessageId)
    {
        cp.ReadUpToMessageId = body.messageId;
        cp.LastReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    // 回 204/200 皆可；前端不需要 payload
    return Results.NoContent();
})
.RequireAuthorization();

// 送出訊息（先支援文字）
// POST /chat/conversations/{id}/messages
app.MapPost("/chat/conversations/{id:long}/messages", async (
    HttpContext ctx,
    AppDbContext db,
    long id,
    SendMessageRequest body) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();
    var me = selfId.Value;
    var now = DateTimeOffset.UtcNow;

    var kind = (body.Kind ?? "text").Trim().ToLowerInvariant();
    if (kind != "text") return Results.BadRequest(new { message = "Only kind=text is supported for now." });
    var text = body.Text?.Trim();
    if (string.IsNullOrEmpty(text)) return Results.BadRequest(new { message = "text required" });

    var cp0 = await db.ConversationParticipants
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.ConversationId == id && x.UserId == me && x.Status == "member");
    if (cp0 is null) return Results.Forbid();

    // 去重
    if (!string.IsNullOrWhiteSpace(body.ClientMessageId))
    {
        var dup = await db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == id
                        && m.SenderId == me
                        && m.ClientMessageId == body.ClientMessageId)
            .OrderByDescending(m => m.MessageId)
            .FirstOrDefaultAsync();

        if (dup is not null)
        {
            return Results.Ok(new MessageItemDto
            {
                Id = dup.MessageId,
                Kind = dup.Kind,
                Text = dup.Text,
                Time = dup.CreatedAt,  // ← 直接回 DateTimeOffset
                Mine = true
            });
        }
    }

    const int MaxAttempts = 3;

    for (var attempt = 1; attempt <= MaxAttempts; attempt++)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var cp = await db.ConversationParticipants
                .FirstOrDefaultAsync(x => x.ConversationId == id && x.UserId == me && x.Status == "member");
            if (cp is null) { await tx.RollbackAsync(); return Results.Forbid(); }

            var stats = await db.ConversationStats
                .FirstOrDefaultAsync(s => s.ConversationId == id);

            if (stats is null)
            {
                stats = new ConversationStats
                {
                    ConversationId = id,
                    NextMessageSeq = 1,
                    MessageCount = 0,
                    MemberCount = 0
                };
                db.ConversationStats.Add(stats);
                await db.SaveChangesAsync();
            }

            var nextSeq = stats.NextMessageSeq <= 0 ? 1 : stats.NextMessageSeq;
            stats.NextMessageSeq = nextSeq + 1;

            var msg = new Message
            {
                ConversationId = id,
                SenderId = me,
                Kind = "text",
                Text = text,
                ReplyToMessageId = body.ReplyToMessageId,
                ClientMessageId = body.ClientMessageId,
                MessageSeq = nextSeq,
                CreatedAt = now
            };
            db.Messages.Add(msg);

            await db.SaveChangesAsync();

            stats.LastMessageId = msg.MessageId;
            stats.LastMessageAt = now;
            stats.LastMessagePreview = BuildPreview(msg.Kind, msg.Text);
            stats.MessageCount = stats.MessageCount + 1;

            cp.ReadUpToMessageId = msg.MessageId;
            cp.LastReadAt = now;

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            var dto = new MessageItemDto
            {
                Id = msg.MessageId,
                Kind = msg.Kind,
                Text = msg.Text,
                Time = msg.CreatedAt,   // ← 直接回 DateTimeOffset
                Mine = true
            };
            return Results.Ok(dto);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            if (attempt == MaxAttempts)
                return Results.Conflict(new { message = "conflict, please retry" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Results.Problem(detail: ex.Message);
        }
    }

    return Results.Problem("unreachable");
}).RequireAuthorization();

// 預覽文字（清單顯示）
static string? BuildPreview(string kind, string? text)
{
    return kind switch
    {
        "text" => Trunc(text, 80),
        "sticker" => "[貼圖]",
        "image" => "[圖片]",
        "file" => "[檔案]",
        _ => "[訊息]"
    };

    static string? Trunc(string? s, int max)
        => string.IsNullOrEmpty(s) ? s : (s!.Length <= max ? s : s[..max]);
}

// 取得會話訊息列表（支援 afterId = readUpToMessageId）
// 回傳 List<MessageItemDto>
app.MapGet("/chat/conversations/{id:long}/messages", async (
    HttpContext ctx,
    AppDbContext db,
    long id,
    int? take,
    long? afterId,   // 往新：> afterId（未讀/更新）
    long? beforeId   // 往舊：< beforeId（歷史）
) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();
    var me = selfId.Value;

    // 必須是成員
    var isMember = await db.ConversationParticipants
        .AsNoTracking()
        .AnyAsync(x => x.ConversationId == id && x.UserId == me && x.Status == "member");
    if (!isMember) return Results.Forbid();

    if (afterId.HasValue && beforeId.HasValue)
        return Results.BadRequest(new { message = "afterId 與 beforeId 不能同時使用" });

    var limit = take.GetValueOrDefault(50);
    if (limit < 1) limit = 1;
    if (limit > 200) limit = 200;

    var q = db.Messages.AsNoTracking()
        .Where(m => m.ConversationId == id && m.DeletedAt == null);

    if (afterId.HasValue)
    {
        // 未讀第一封開始（不含 afterId 本身）
        q = q.Where(m => m.MessageId > afterId.Value)
             .OrderBy(m => m.MessageSeq)
             .ThenBy(m => m.MessageId)
             .Take(limit);
    }
    else if (beforeId.HasValue)
    {
        // 歷史訊息（不含 beforeId 本身）
        q = q.Where(m => m.MessageId < beforeId.Value)
             .OrderByDescending(m => m.MessageId) // 先新→舊取
             .Take(limit);
    }
    else
    {
        // 沒帶游標：最新 N 筆
        q = q.OrderByDescending(m => m.MessageId).Take(limit);
    }

    var list = await q.Select(m => new MessageItemDto
    {
        Id   = m.MessageId,
        Kind = m.Kind,
        Text = m.Text,
        Time = m.CreatedAt,   // DateTimeOffset → ISO 8601
        Mine = (m.SenderId == me)
    }).ToListAsync();

    // beforeId 或預設最新取法，回傳前翻成舊→新，方便前端直接渲染
    if (beforeId.HasValue || (!afterId.HasValue && !beforeId.HasValue))
        list.Reverse();

    return Results.Ok(list);
})
.RequireAuthorization();

// 取 DM 對方的已讀游標（ReadUpToMessageId）與時間
app.MapGet("/chat/conversations/{id:long}/dm-peer-read", async (
    HttpContext ctx,
    AppDbContext db,
    long id
) =>
{
    var selfId = GetUserId(ctx);
    if (selfId is null) return Results.Unauthorized();
    var me = selfId.Value;

    // 呼叫者必須是成員
    var isMember = await db.ConversationParticipants
        .AsNoTracking()
        .AnyAsync(x => x.ConversationId == id && x.UserId == me && x.Status == "member");
    if (!isMember) return Results.Forbid();

    // 僅支援 dm
    var convType = await db.Conversations
        .AsNoTracking()
        .Where(c => c.ConversationId == id)
        .Select(c => c.Type)
        .FirstOrDefaultAsync();

    if (!string.Equals(convType, "dm", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { message = "Only dm supported." });

    var now = DateTimeOffset.UtcNow;

    // 找對方成員
    var peer = await db.ConversationParticipants
        .AsNoTracking()
        .Where(cp => cp.ConversationId == id && cp.UserId != me && cp.Status == "member")
        .Select(cp => new DmPeerReadDto
        {
            PeerUserId = cp.UserId,
            ReadUpToMessageId = cp.ReadUpToMessageId,
            LastReadAt = cp.LastReadAt,
            ServerNow = now
        })
        .FirstOrDefaultAsync();

    // 若對方不存在（理論上不會），回傳空值
    peer ??= new DmPeerReadDto
    {
        PeerUserId = null,
        ReadUpToMessageId = null,
        LastReadAt = null,
        ServerNow = now
    };

    return Results.Ok(peer);
})
.RequireAuthorization();



app.Run();
// === record 型別必須放在最後 ===
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 9.0 / 5.0 + 32);
}

public record CreateFriendReq(string ToUserId, string? Message);
// 回傳模型
public sealed class DmPeerReadDto
{
    public long? PeerUserId { get; set; }
    public long? ReadUpToMessageId { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public DateTimeOffset ServerNow { get; set; }
}