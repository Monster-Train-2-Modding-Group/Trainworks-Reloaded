using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trigger
{
    public class CardTriggerEffectRegister
        : Dictionary<string, CardTriggerEffectData>,
            IRegister<CardTriggerEffectData>
    {
        private readonly IModLogger<CardTriggerEffectRegister> logger;

        public CardTriggerEffectRegister(IModLogger<CardTriggerEffectRegister> logger)
        {
            this.logger = logger;
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

        public void Register(string key, CardTriggerEffectData item)
        {
            logger.Log(LogLevel.Debug, $"Register Card Trigger Effect ({key})");
            Add(key, item);
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CardTriggerEffectData? lookup, [NotNullWhen(true)] out bool? IsModded)
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
