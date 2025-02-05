using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Config;
using RootAlert.Processing;


namespace RootAlert.Middleware
{
    public static class RootAlertExtensions
    {
        public static IServiceCollection AddRootAlert(this IServiceCollection services, RootAlertOptions options)
        {
            services.AddSingleton(options);
            services.AddSingleton<RootAlertProcessor>();

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

        public static IApplicationBuilder UseRootAlert(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RootAlertMiddleware>();
        }
    }
}