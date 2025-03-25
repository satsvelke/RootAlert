using RootAlert.Storage;
using RootAlert.Storage.Memory;

namespace RootAlert.Config
{
    public class RootAlertSetting
    {
        public IRootAlertStorage Storage { get; set; } = new MemoryAlertStorage();
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMinutes(30);
        public IList<RootAlertOption>? RootAlertOptions { get; set; }
    }
}
