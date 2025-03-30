using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Processing;

namespace RootAlert.Middleware
{
    public sealed class RootAlertMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RootAlertMiddleware> _logger;
        private readonly RootAlertProcessor _processor;

        public RootAlertMiddleware(RequestDelegate next, ILogger<RootAlertMiddleware> logger, RootAlertProcessor processor)
        {
            _next = next;
            _logger = logger;
            _processor = processor;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                HandleExceptionAsync(context, ex);
                throw;
            }
        }

        private void HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _processor.AddToBatch(exception, context);
        }
    }
}
