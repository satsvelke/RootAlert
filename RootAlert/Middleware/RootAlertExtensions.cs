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
        public static IServiceCollection AddRootAlert(this IServiceCollection services, RootAlertSetting rootAlertSetting)
        {
            services.AddSingleton(rootAlertSetting);

            services.AddSingleton<IEnumerable<RootAlertOption>>(rootAlertSetting.RootAlertOptions!);
            services.AddSingleton<RootAlertProcessor>();

            services.AddSingleton<IAlertService>(provider =>
            {
                var alertServices = new List<IAlertService>();
                var options = provider.GetRequiredService<IEnumerable<RootAlertOption>>().ToList();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                foreach (var option in options)
                {
                    switch (option.AlertMethod)
                    {
                        case AlertType.Slack:
                            alertServices.Add(new SlackAlertService(option.WebhookUrl!, loggerFactory.CreateLogger<SlackAlertService>()));
                            break;
                        case AlertType.Teams:
                            alertServices.Add(new TeamsAlertService(option.WebhookUrl!, loggerFactory.CreateLogger<TeamsAlertService>()));
                            break;
                        case AlertType.Email:
                            alertServices.Add(new EmailAlertService(option.EmailSettings!, loggerFactory.CreateLogger<EmailAlertService>()));
                            break;
                    }
                }

                return new MultiAlertService(alertServices);
            });

            return services;
        }


        public static IApplicationBuilder UseRootAlert(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RootAlertMiddleware>();
        }
    }
}
