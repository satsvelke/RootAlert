using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sherlog.Alerts;
using SherLog.Processing;
using System.Net;

namespace SherLog.Middleware
{
    public class SherLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SherLogMiddleware> _logger;
        private readonly IAlertService _alertService;
        private readonly SherLogProcessor _processor;

        public SherLogMiddleware(RequestDelegate next, ILogger<SherLogMiddleware> logger, IAlertService alertService, SherLogProcessor processor)
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


            _processor.AddToBatch(exception);

            // Send batch alert if time threshold met
            if (_processor.ShouldSendBatch())
            {
                string batchMessage = _processor.GetBatchSummaryMarkdown();
                if (!string.IsNullOrEmpty(batchMessage))
                {
                    await _alertService.SendAlertAsync(batchMessage);
                }
            }
        }
    }
}
