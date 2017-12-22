using System;
namespace ObservableMessaging.Core.Interfaces
{
    public interface ILog
    {
        void Log(string message, LogLevel level = LogLevel.Info);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Critical
    }
}
