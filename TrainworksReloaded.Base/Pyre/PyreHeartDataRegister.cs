using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Pyre
{
    public class PyreHeartDataRegister : Dictionary<string, PyreHeartData>, IRegister<PyreHeartData>
    {
        private readonly IModLogger<PyreHeartDataRegister> logger;

        public PyreHeartDataRegister(IModLogger<PyreHeartDataRegister> logger)
        {
            this.logger = logger;
        }


        public void Register(string key, PyreHeartData item)
        {
            logger.Log(LogLevel.Debug, $"Register Pyre Heart {key}");
            Add(key, item);
        }


        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out PyreHeartData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }

    }
}
