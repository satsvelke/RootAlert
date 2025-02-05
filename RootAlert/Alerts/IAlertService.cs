

namespace RootAlert.Alerts;

public interface IAlertService
{
    Task SendAlertAsync(string exception);
}
