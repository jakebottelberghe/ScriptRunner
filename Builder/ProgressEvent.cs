using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Builder
{
    internal class ProgressEvent
    {
        public string Pipeline { get; set; }
        public string Step { get; set; }
        public int Action { get; set; }
        public int Result { get; set; }
        public bool WasFilteredByResult { get; set; }
        public LogLevel LogLevel { get; set; }
    }
}
