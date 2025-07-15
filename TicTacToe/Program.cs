using Microsoft.EntityFrameworkCore;
using TicTacToe.Data;
using TicTacToe.Repositories;
using TicTacToe.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddScoped<GameRepository>();
builder.Services.AddScoped<GameService>();


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok());
app.MapControllers();

app.UseStaticFiles();

app.Run();
