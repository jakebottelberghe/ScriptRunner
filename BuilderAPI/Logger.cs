using BuilderAPIData;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPI
{
    internal class Logger
    {
        public List<LoggerRecord> Always { get; set; } = new();
        public List<LoggerRecord> Trace { get; set; } = new();
        public List<LoggerRecord> Debug { get; set; } = new();
        public List<LoggerRecord> Information { get; set; } = new();
        public List<LoggerRecord> Warning { get; set; } = new();
        public List<LoggerRecord> Error { get; set; } = new();
        public List<LoggerRecord> Critical { get; set; } = new();
        public List<string> Tags { get; set; } = new();

        public Action<LogLevel, string, Exception?, object?[]> LogFunc { get; set; }

        public Logger()
        {
            LogFunc = Log;
        }

        internal void Log(LogLevel level, string message, Exception? expection = null, params object?[] param)
        {
            RunLog(Always, level, message, expection, param);

            switch(level)
            {
                case LogLevel.Trace: RunLog(Trace, level, message, expection, param); break;
                case LogLevel.Debug: RunLog(Debug, level, message, expection, param); break;
                case LogLevel.Information: RunLog(Information, level, message, expection, param); break;
                case LogLevel.Warning: RunLog(Warning, level, message, expection, param); break;
                case LogLevel.Error: RunLog(Error, level, message, expection, param); break;
                case LogLevel.Critical: RunLog(Critical, level, message, expection, param); break;
            }
        }

        private void RunLog(List<LoggerRecord> loggers, LogLevel level, string message, Exception? expection, object?[] param)
        {
            foreach(var log in loggers)
            {
                log.Log(level, message, expection, param);
            }
        }
    }
}
