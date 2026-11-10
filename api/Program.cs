using ManagementPanel.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PanelDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Panel");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Set ConnectionStrings__Panel to use the PostgreSQL database.");
    }

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

app.MapGet("/api/health", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { status = "ok" });
});

app.Run();
