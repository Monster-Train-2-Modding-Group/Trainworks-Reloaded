using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Room
{
    public class RoomModifierRegister
        : Dictionary<string, RoomModifierData>,
            IRegister<RoomModifierData>
    {
        private readonly IModLogger<RoomModifierRegister> logger;

        public RoomModifierRegister(IModLogger<RoomModifierRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, RoomModifierData item)
        {
            logger.Log(LogLevel.Debug, $"Register Room Modifier ({key})");
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out RoomModifierData? lookup, [NotNullWhen(true)] out bool? IsModded)
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
