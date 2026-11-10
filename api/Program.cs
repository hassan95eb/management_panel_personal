var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/health", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { status = "ok" });
});

app.Run();
