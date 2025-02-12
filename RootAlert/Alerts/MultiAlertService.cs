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

        public async Task SendBatchAlertAsync(IList<ErrorLogEntry> errors)
        {
            var tasks = _alertServices.Select(service => service.SendBatchAlertAsync(errors));
            await Task.WhenAll(tasks);
        }
    }
}
