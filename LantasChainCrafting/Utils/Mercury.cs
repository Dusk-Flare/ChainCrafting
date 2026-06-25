using BepInEx.Logging;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChainCrafting.Utils
{
    public class Mercury : ILogSource, IDisposable
    {
        public string SourceName { get; }
        public event EventHandler<LogEventArgs> LogEvent;

        public Mercury(string sourceName)
        {
            SourceName = sourceName;
            Logger.Sources.Add(this);
        }

        public void Log(LogLevel level, string callerName, string callerClass, object data)
        {
            LogEvent?.Invoke(this, new MercuryEntry(callerName, callerClass, data, level, this));
        }

        public void LogFatal(object data, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Fatal, memberName, CallerPath(filePath), data);
        }

        public void LogError(object data, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Error, memberName, CallerPath(filePath), data);
        }

        public void LogWarning(object data, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Warning, memberName, CallerPath(filePath), data);
        }

        public void LogMessage(object data, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Message, memberName, CallerPath(filePath), data);
        }

        public void LogInfo(object data, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Info, memberName, CallerPath(filePath), data);
        }

        public void LogDebug(object data, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            Log(LogLevel.Debug, memberName, CallerPath(filePath), data);
        }

        public void LogCatch<TException>(TException exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "") where TException : Exception
        {
            Log(LogLevel.Debug, memberName, CallerPath(filePath), exception);
        }

        public void LogException<TException>(TException exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "") where TException : Exception
        {
            Log(LogLevel.Debug, memberName, CallerPath(filePath), exception);
            throw exception;
        }

        public void Dispose()
        {
        }

        private static string CallerPath(string filePath) => Path.GetFileNameWithoutExtension(filePath);
        private class MercuryEntry : LogEventArgs
        {
            public string CallerName { get; }
            public string CallerClass { get; }
            public MercuryEntry(string callerName, string callerClass, object data, LogLevel level, ILogSource source) : base(data, level, source) 
            { 
                CallerName = callerName; 
                CallerClass = callerClass;
            }

            public override string ToString()
            {
                StringBuilder builder = new();
                builder.Append($"\n[{ Level,-7}:{ Source.SourceName,10}]");
                string prefix = $"\n[At {CallerName,-7}:{CallerClass,7}]: ";
                if (Data is Exception ex) Data = ex.StackTrace;
                string indent = "\n".PadRight(prefix.Length);
                string[] lines = Data.ToString().Split('\n')
                    .SkipWhile(string.IsNullOrEmpty).Reverse()
                    .SkipWhile(string.IsNullOrEmpty).Reverse().ToArray();
                foreach (string line in lines)
                {
                    builder.Append(prefix).Append(line);
                    prefix = indent;
                }
                return builder.ToString();
            }
        }
    }
}
