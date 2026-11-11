using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

namespace ManagementPanel.Api.Codex;

internal static class CodexRequestLimits
{
    internal const string Policy = "codex-key";
    private const long MaxBodyBytes = 65_536;

    internal static void Configure(RateLimiterOptions options, IConfiguration configuration)
    {
        var ingressLimit = Math.Max(1, configuration.GetValue("Codex:RequestsPerMinute", 300));
        var keyLimit = Math.Max(1, configuration.GetValue("Codex:RequestsPerKeyPerMinute", 60));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            IsCodex(context)
                ? Window("codex-ingress", ingressLimit)
                : RateLimitPartition.GetNoLimiter("other"));
        options.AddPolicy(Policy, context =>
            Window(CodexBearerKey.ReadHash(context.Request) ?? "missing", keyLimit));
        options.OnRejected = (context, _) =>
        {
            if (IsCodex(context.HttpContext) &&
                context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers["Retry-After"] =
                    Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

            return ValueTask.CompletedTask;
        };
    }

    internal static void UseCodexRequestLimits(this WebApplication app)
    {
        app.UseStatusCodePages(async statusContext =>
        {
            var context = statusContext.HttpContext;
            if (!IsCodex(context)) return;

            var status = context.Response.StatusCode;
            var title = status switch
            {
                400 => "Invalid request JSON, activity fields, task, or idempotency key.",
                401 => "A valid, active Codex bearer key is required.",
                404 => "The requested resource is unavailable to this key.",
                409 => "The idempotency key was already used with different data.",
                413 => "The request body exceeds 64 KiB.",
                415 => "The request body must be JSON.",
                429 => "The request limit was reached. Retry later.",
                _ => "The request could not be completed."
            };

            await Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
        });

        app.Use(async (context, next) =>
        {
            if (IsCodex(context) && HttpMethods.IsPost(context.Request.Method))
            {
                if (context.Request.ContentLength > MaxBodyBytes)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    return;
                }

                // Kestrel also enforces the limit when the request uses chunked encoding.
                var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (feature is { IsReadOnly: false } &&
                    (feature.MaxRequestBodySize is null || feature.MaxRequestBodySize > MaxBodyBytes))
                    feature.MaxRequestBodySize = MaxBodyBytes;
            }

            await next(context);
        });
    }

    private static bool IsCodex(HttpContext context) => context.Request.Path.StartsWithSegments("/api/codex");

    private static RateLimitPartition<string> Window(string partition, int limit) =>
        RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
}
