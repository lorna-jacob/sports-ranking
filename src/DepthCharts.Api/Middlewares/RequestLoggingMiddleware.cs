using System.Diagnostics;

namespace DepthCharts.Api.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for static files and health checks
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.Value?.Contains('.') == true)
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Request {Method} {Path} started. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

                _logger.Log(logLevel,
                    "Request {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    context.TraceIdentifier);
            }
        }
    }
}
