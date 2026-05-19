using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainCrafting.Utils
{
    public class Chronos
    {
        private ManualLogSource Logger { init; get; }

        public Chronos(ManualLogSource logger)
        {
            Logger = logger;
        }

        public void LogInfo(Type sender, object data)
        {
            Logger.LogInfo($"{data} [{sender.FullName}]");
        }

        public static implicit operator Chronos(ManualLogSource logger) => new(logger);
    }
}
