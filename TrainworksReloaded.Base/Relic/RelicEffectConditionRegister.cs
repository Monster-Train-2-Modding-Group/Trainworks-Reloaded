using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectConditionRegister : Dictionary<string, RelicEffectCondition>, IRegister<RelicEffectCondition>
    {
        private readonly IModLogger<RelicEffectConditionRegister> logger;

        public RelicEffectConditionRegister(GameDataClient client, IModLogger<RelicEffectConditionRegister> logger)
        {
            this.logger = logger;
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

        public void Register(string key, RelicEffectCondition item)
        {
            logger.Log(LogLevel.Info, $"Register RelicEffectCondition {key}... ");
            Add(key, item);
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out RelicEffectCondition? lookup, [NotNullWhen(true)] out bool? IsModded)
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