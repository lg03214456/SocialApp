var builder = WebApplication.CreateBuilder(args);

// Swagger（Swashbuckle）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS（前端 Vite 用）
const string CorsPolicy = "_cors";
builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p => p
        .WithOrigins("http://localhost:5173")
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

app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => (int)Math.Round(TemperatureC * 9.0 / 5.0 + 32);
}