using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Trigger
{
    public class CharacterTriggerFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CharacterTriggerFinalizer> logger;
        private readonly IRegister<CardEffectData> effectRegister;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly ICache<IDefinition<CharacterTriggerData>> cache;

        public CharacterTriggerFinalizer(
            IModLogger<CharacterTriggerFinalizer> logger,
            IRegister<CardEffectData> effectRegister,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister,
            IRegister<StatusEffectData> statusRegister,
            ICache<IDefinition<CharacterTriggerData>> cache
        )
        {
            this.logger = logger;
            this.effectRegister = effectRegister;
            this.triggerEnumRegister = triggerEnumRegister;
            this.statusRegister = statusRegister;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCharacterTrigger(definition);
            }
            cache.Clear();
        }

        private void FinalizeCharacterTrigger(IDefinition<CharacterTriggerData> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var data = definition.Data;

            logger.Log(LogLevel.Info,
                $"Finalizing Character Trigger {definition.Key} {definition.Id} path: {configuration.GetPath()}..."
            );

            //handle trigger
            var trigger = CharacterTriggerData.Trigger.OnDeath;
            var triggerReference = configuration.GetSection("trigger").ParseReference();
            if (triggerReference != null)
            {
                if (
                    triggerEnumRegister.TryLookupId(
                        triggerReference.ToId(key, TemplateConstants.CharacterTriggerEnum),
                        out var triggerFound,
                        out var _,
                        triggerReference.context
                    )
                )
                {
                    trigger = triggerFound;
                }
            }
            AccessTools
                .Field(typeof(CharacterTriggerData), "trigger")
                .SetValue(data, trigger);

            //handle effects cards
            var effectDatas = new List<CardEffectData>();
            var effectReferences = configuration
                .GetSection("effects")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in effectReferences)
            {
                if (
                    effectRegister.TryLookupId(
                        reference.ToId(key, TemplateConstants.Effect),
                        out var effect,
                        out var _,
                        reference.context
                    )
                )
                {
                    effectDatas.Add(effect);
                }
            }
            AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(data, effectDatas);

            var requiredStatusEffects = data.GetRequiredStatusEffects() ?? [];
            foreach (var child in configuration.GetSection("required_status_effects").GetChildren())
            {
                var reference = child.GetSection("status").ParseReference();
                if (reference == null)
                    continue;
                var statusEffectId = reference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, reference.context))
                {
                    requiredStatusEffects.Add(new StatusEffectStackData()
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child.GetSection("count").ParseInt() ?? 0,
                        fromPermanentUpgrade = child.GetSection("from_permanent_upgrade").ParseBool() ?? false
                    });
                }
            }
            AccessTools
                .Field(typeof(CharacterTriggerData), "requiredStatusEffects")
                .SetValue(data, requiredStatusEffects);
        }
    }
}
