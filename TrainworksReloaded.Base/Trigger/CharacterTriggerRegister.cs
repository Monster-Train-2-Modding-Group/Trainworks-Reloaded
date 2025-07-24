using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trigger
{
    public class CharacterTriggerRegister
        : Dictionary<string, CharacterTriggerData>,
            IRegister<CharacterTriggerData>
    {
        private readonly IModLogger<CharacterTriggerRegister> logger;

        public CharacterTriggerRegister(IModLogger<CharacterTriggerRegister> logger)
        {
            this.logger = logger;
        }


        public void Register(string key, CharacterTriggerData item)
        {
            logger.Log(LogLevel.Debug, $"Register Character Trigger ({key})");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Values.Select(trigger => trigger.GetDebugName())],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CharacterTriggerData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var trigger in this.Values)
                    {
                        if (trigger.GetDebugName() == identifier)
                        {
                            lookup = trigger;
                            IsModded = true;
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }

    }
}
