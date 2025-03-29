using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Config;
using RootAlert.Processing;
using RootAlert.Storage;

namespace RootAlert.Middleware
{
    public static class RootAlertExtensions
    {
        public static IServiceCollection AddRootAlert(this IServiceCollection services, RootAlertSetting rootAlertSetting)
        {
            services.AddSingleton(rootAlertSetting);

            services.AddSingleton<IEnumerable<RootAlertOption>>(rootAlertSetting.RootAlertOptions!);
            services.AddSingleton<RootAlertProcessor>();

            services.AddSingleton<IRootAlertStorage>(rootAlertSetting.Storage);


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
                            alertServices.Add(new SlackAlertService(loggerFactory.CreateLogger<SlackAlertService>(), rootAlertSetting));
                            break;
                        case AlertType.Teams:
                            alertServices.Add(new TeamsAlertService(loggerFactory.CreateLogger<TeamsAlertService>(), rootAlertSetting));
                            break;
                        case AlertType.SMTPEmail:
                            alertServices.Add(new EmailAlertService(loggerFactory.CreateLogger<EmailAlertService>(), rootAlertSetting));
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
