using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPIData
{
    public record LoggerRecord(Action<LogLevel, string, Exception?, object?[]> Log, List<string> Tags);
}
