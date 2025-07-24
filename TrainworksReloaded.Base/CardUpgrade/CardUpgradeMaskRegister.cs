using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeMaskRegister
        : Dictionary<string, CardUpgradeMaskData>,
            IRegister<CardUpgradeMaskData>
    {
        private readonly IModLogger<CardUpgradeMaskRegister> logger;
        private readonly Lazy<SaveManager> saveManager;

        public CardUpgradeMaskRegister(IModLogger<CardUpgradeMaskRegister> logger, GameDataClient client)
        {
            saveManager = new Lazy<SaveManager>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    return provider;
                }
                else
                {
                    return new SaveManager();
                }
            });
            this.logger = logger;
        }

        public void Register(string key, CardUpgradeMaskData item)
        {
            logger.Log(LogLevel.Debug, $"Registering Upgrade Mask {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Values.Select(mask => mask.name)],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CardUpgradeMaskData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = false;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var mask in this.Values)
                    {
                        if (mask.name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = mask;
                            IsModded = true;
                            return true;
                        }
                    }
                    lookup = GetVanillaCardUpgradeMask(identifier);
                    return lookup != null;
                case RegisterIdentifierType.GUID:
                    bool ret = TryGetValue(identifier, out lookup);
                    IsModded = ret;
                    if (ret == false)
                    {
                        lookup = GetVanillaCardUpgradeMask(identifier);
                        return lookup != null;
                    }
                    return ret;
            }
            return false;
        }

        public CardUpgradeMaskData? GetVanillaCardUpgradeMask(string maskName)
        {
            return GetVanillaCardUpgradeMask(saveManager.Value.GetAllGameData(), maskName);
        }

        public static CardUpgradeMaskData? GetVanillaCardUpgradeMask(AllGameData allGameData, string maskName)
        {
            foreach (var enhancer in allGameData.GetAllEnhancerData())
            {
                foreach (var effect in enhancer.GetEffects())
                {
                    if (effect.GetParamCardFilter()?.name == maskName)
                        return effect.GetParamCardFilter();
                    if (effect.GetParamCardFilterSecondary()?.name == maskName)
                        return effect.GetParamCardFilterSecondary();

                    var filters = effect.GetParamCardUpgradeData()?.GetFilters();
                    if (filters.IsNullOrEmpty())
                        continue;
                    foreach (var filter in filters!)
                    {
                        if (filter.name == maskName)
                            return filter;
                    }
                }
            }
            return null;
        }

    }
}
