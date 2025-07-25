using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.StatusEffects
{
    public class StatusEffectDataRegister : Dictionary<string, StatusEffectData>, IRegister<StatusEffectData>
    {
        private readonly IModLogger<StatusEffectDataRegister> logger;

        public StatusEffectDataRegister(IModLogger<StatusEffectDataRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, StatusEffectData item)
        {
            logger.Log(LogLevel.Info, $"Register Status Effect ({key})");
            StatusEffectManager.Instance.GetAllStatusEffectsData().GetStatusEffectData().Add(item);
            StatusEffectManager.StatusIdToLocalizationExpression.Add(item.GetStatusId(), "StatusEffect_" + item.GetStatusId());
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. StatusEffectManager.Instance.GetAllStatusEffectsData().GetStatusEffectData().Select(effect => effect.GetStatusId())],
                RegisterIdentifierType.GUID => [.. StatusEffectManager.Instance.GetAllStatusEffectsData().GetStatusEffectData().Select(effect => effect.GetStatusId())],
                _ => [],
            };
        }


        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out StatusEffectData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            // StatusIDs are all lowercase.
            identifier = identifier.ToLowerInvariant();
            IsModded = ContainsKey(identifier);
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var effect in StatusEffectManager.Instance.GetAllStatusEffectsData().GetStatusEffectData())
                    {
                        if (effect.GetStatusId() == identifier)
                        {
                            lookup = effect;
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    foreach (var effect in StatusEffectManager.Instance.GetAllStatusEffectsData().GetStatusEffectData())
                    {
                        if (effect.GetStatusId() == identifier)
                        {
                            lookup = effect;
                            return true;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}
