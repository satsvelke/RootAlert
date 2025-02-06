
namespace RootAlert.Alerts
{
    public class MultiAlertService : IAlertService
    {
        private readonly List<IAlertService> _alertServices;

        public MultiAlertService(List<IAlertService> alertServices)
        {
            _alertServices = alertServices;
        }

        public async Task SendAlertAsync(string message)
        {
            var tasks = new List<Task>();

            foreach (var service in _alertServices)
            {
                tasks.Add(service.SendAlertAsync(message));
            }

            await Task.WhenAll(tasks);
        }
    }
}
