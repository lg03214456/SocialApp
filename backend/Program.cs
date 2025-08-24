using Microsoft.EntityFrameworkCore;
using SocialApp.Data;
using SocialApp.Models;
var builder = WebApplication.CreateBuilder(args);

// EF Core 註冊（讀 DefaultConnection）
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Swagger 方便測試
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS（前端 Vite 用）
const string CorsPolicy = "_cors";
builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p => p
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FriendApp API v1");
    c.RoutePrefix = "swagger";
});

// app.UseHttpsRedirection();
app.UseCors(CorsPolicy);

// --- 範例 API ---
var summaries = new[] { "Freezing","Bracing","Chilly","Cool","Mild","Warm","Balmy","Hot","Sweltering","Scorching" };


// DB 連線測試
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
})
.WithName("GetWeatherForecast"); // ← 這裡不再呼叫 .WithOpenApi()

// Friends CRUD（最小）
app.MapGet("/friends", async (AppDbContext db) => await db.Friends.ToListAsync());
app.MapPost("/friends", async (AppDbContext db, Friend f) =>
{
    db.Friends.Add(f);
    await db.SaveChangesAsync();
    return Results.Created($"/friends/{f.Id}", f);
});


app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 9.0 / 5.0 + 32);
}