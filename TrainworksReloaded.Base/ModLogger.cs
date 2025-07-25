using BepInEx.Logging;
using ShinyShoe.Logging;
using System;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base
{
    public class ModLoggerWrapper(Type type, object data)
    {
        private readonly Type type = type;
        private readonly object data = data;

        public override string ToString()
        {
            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.ffffff");
            return $"{$"[{timestamp}] [{type.Name}]",-32} {data}";
        }
    }

    public class ModLogger<T>(ManualLogSource manualLogSource) : IModLogger<T>, ILogProvider
    {
        private readonly ManualLogSource manualLogSource = manualLogSource;

        public void CloseLog() { }

        public void Debug(string log, LogOptions options)
        {
            Log(Core.Interfaces.LogLevel.Debug, log);
        }

        public void Error(string log, LogOptions options)
        {
            Log(Core.Interfaces.LogLevel.Error, log);
        }

        public void Info(string log, LogOptions options)
        {
            Log(Core.Interfaces.LogLevel.Info, log);
        }

        public void Log(Core.Interfaces.LogLevel level, object data)
        {
            manualLogSource.Log(
                (BepInEx.Logging.LogLevel)(int)level,
                new ModLoggerWrapper(typeof(T), data)
            );
        }

        public void Verbose(string log, LogOptions options)
        {
            Log(Core.Interfaces.LogLevel.Debug, log);
        }

        public void Warning(string log, LogOptions options)
        {
            Log(Core.Interfaces.LogLevel.Warning, log);
        }
    }
}
