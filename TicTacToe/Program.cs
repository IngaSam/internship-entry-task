using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicTacToe.Data;
using TicTacToe.Repositories;
using TicTacToe.Services;
using TicTacToe.Services.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddScoped<GameRepository>();
builder.Services.AddScoped<IRandomProvider, DefaultRandomProvider>();
builder.Services.AddScoped<GameService>();

var app = builder.Build();
app.Logger.LogInformation("Starting application");
try 
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Applying database migrations...");
    db.Database.Migrate();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database migration failed");
    throw;
}


app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok());
app.MapControllers();

app.UseStaticFiles();

app.Run();
