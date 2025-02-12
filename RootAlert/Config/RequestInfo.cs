using System;

namespace RootAlert.Config;

public record RequestInfo(string Url, string Method, Dictionary<string, string> Headers);

