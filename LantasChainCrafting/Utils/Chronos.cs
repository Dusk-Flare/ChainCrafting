using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChainCrafting.Utils
{
    internal class Chronos : ManualLogSource
    {
        private readonly string Owner;
        private new event EventHandler<LogEventArgs> LogEvent;

        public Chronos(string owner, ManualLogSource logger) : base(logger.SourceName)
        {
            Owner = owner;
            Logger.Sources.Add(this);
        }

        public new void Log(LogLevel level, object data)
        {
            LogEvent?.Invoke(this, new ChronosEntry(Owner, data, level, this));
        }

        private class ChronosEntry : LogEventArgs
        {
            private readonly string owner;
            public ChronosEntry(string owner, object data, LogLevel level, ILogSource source) : base(data, level, source) { this.owner = owner; }

            public override string ToString()
            {
                StringBuilder builder = new();
                string prefix = $"[{Level,-7}:{Source.SourceName,10}|{owner}]: ";
                if (Data is Exception ex) Data = ex.StackTrace;
                string indent = "\n".PadRight(prefix.Length);
                foreach (string line in Data.ToString().Split('\n'))
                {
                    builder.Append(prefix).AppendLine(line);
                    prefix = indent;
                }
                return builder.ToString();
            }
        }
    }
}
