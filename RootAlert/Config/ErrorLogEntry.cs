namespace RootAlert.Config
{
    public class ErrorLogEntry
    {
        public int Count { get; set; }

        public ExceptionInfo? Exception { get; set; }

        public RequestInfo? Request { get; set; }
    }
}
