using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sherlog.Alerts;
using Sherlog.Config;
using SherLog.Config;
using SherLog.Processing;


namespace SherLog.Middleware
{
    public static class SherLogExtensions
    {
        public static IServiceCollection AddSherLog(this IServiceCollection services, SherLogOptions options)
        {
            services.AddSingleton(options);
            services.AddSingleton<SherLogProcessor>();

            if (options.AlertMethod == AlertType.Slack)
            {
                services.AddSingleton<IAlertService>(provider =>
                    new SlackAlertService(options.WebhookUrl!, provider.GetRequiredService<ILogger<SlackAlertService>>()));
            }

            if (options.AlertMethod == AlertType.Teams)
            {
                services.AddSingleton<IAlertService>(provider =>
                    new TeamsAlertService(options.WebhookUrl!, provider.GetRequiredService<ILogger<TeamsAlertService>>()));
            }


            if (options.AlertMethod == AlertType.Email)
            {
                services.AddSingleton<IAlertService>(provider =>
                    new EmailAlertService(options.EmailSettings!, provider.GetRequiredService<ILogger<EmailAlertService>>()));
            }

            return services;
        }

        public static IApplicationBuilder UseSherLog(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SherLogMiddleware>();
        }
    }
}