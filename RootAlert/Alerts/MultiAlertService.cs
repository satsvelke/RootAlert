using Microsoft.AspNetCore.Http;
using RootAlert.Config;

namespace RootAlert.Alerts
{
    public class MultiAlertService : IAlertService
    {
        private readonly List<IAlertService> _alertServices;

        public MultiAlertService(List<IAlertService> alertServices)
        {
            _alertServices = alertServices;
        }

        public async Task SendAlertAsync(Exception exception, HttpContext context)
        {
            var tasks = _alertServices.Select(service => service.SendAlertAsync(exception, context));
            await Task.WhenAll(tasks);
        }

        public async Task SendBatchAlertAsync(List<(int count, Exception exception, RequestInfo requestInfo)> errors)
        {
            var tasks = _alertServices.Select(service => service.SendBatchAlertAsync(errors));
            await Task.WhenAll(tasks);
        }
    }
}
