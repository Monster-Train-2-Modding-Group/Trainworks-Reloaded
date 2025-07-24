using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Tooltips
{
    public class AdditionalTooltipRegister : Dictionary<string, AdditionalTooltipData>, IRegister<AdditionalTooltipData>
    {
        private readonly IModLogger<AdditionalTooltipRegister> logger;

        public AdditionalTooltipRegister(IModLogger<AdditionalTooltipRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, AdditionalTooltipData item)
        {
            logger.Log(LogLevel.Debug, $"Register Additional Tooltip {key}");
            this.Add(key, item);
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out AdditionalTooltipData? lookup, [NotNullWhen(true)] out bool? IsModded)
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
