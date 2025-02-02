using System;

namespace Sherlog.Alerts;

public interface IAlertService
{
    Task SendAlertAsync(string exception);
}
