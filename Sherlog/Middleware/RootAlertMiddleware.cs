using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Processing;

namespace RootAlert.Middleware
{
    public class RootAlertMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RootAlertMiddleware> _logger;
        private readonly IAlertService _alertService;
        private readonly RootAlertProcessor _processor;

        public RootAlertMiddleware(RequestDelegate next, ILogger<RootAlertMiddleware> logger, IAlertService alertService, RootAlertProcessor processor)
        {
            _next = next;
            _logger = logger;
            _alertService = alertService;
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
                _ = HandleExceptionAsync(context, ex);

                await _next(context);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {


            _processor.AddToBatch(exception, context);

            // Send batch alert if time threshold met
            if (_processor.ShouldSendBatch())
            {
                string batchMessage = _processor.GetBatchSummary();
                if (!string.IsNullOrEmpty(batchMessage))
                {
                    await _alertService.SendAlertAsync(batchMessage);
                }
            }
        }
    }
}
