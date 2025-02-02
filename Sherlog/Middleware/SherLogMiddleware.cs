using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace SherLog.Middleware
{
    public class SherLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SherLogMiddleware> _logger;

        public SherLogMiddleware(RequestDelegate next, ILogger<SherLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An unexpected error occurred.",
                Details = exception.Message
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);

            await SendAlertAsync(exception);
        }

        private Task SendAlertAsync(Exception exception)
        {
            Console.WriteLine(exception.Message);
            return Task.CompletedTask;
        }
    }
}