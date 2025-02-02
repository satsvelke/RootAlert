using System;

namespace Sherlog.Alerts;

internal interface IAlertService
{
    Task SendAlertAsync(string message);
}
