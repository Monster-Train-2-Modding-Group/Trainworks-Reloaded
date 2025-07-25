using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trait
{
    public class CardTraitDataRegister : Dictionary<string, CardTraitData>, IRegister<CardTraitData>
    {
        private readonly IModLogger<CardTraitDataRegister> logger;

        public CardTraitDataRegister(IModLogger<CardTraitDataRegister> logger)
        {
            this.logger = logger;
        }


        public void Register(string key, CardTraitData item)
        {
            logger.Log(LogLevel.Debug, $"Register Trait {key}");
            Add(key, item);
        }


        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Values.Select(trait => trait.traitStateName)],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CardTraitData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var trait in this.Values)
                    {
                        if (trait.traitStateName == identifier)
                        {
                            lookup = trait;
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
