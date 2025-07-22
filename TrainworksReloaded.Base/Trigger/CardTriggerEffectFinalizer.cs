using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using HarmonyLib;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Trigger
{
    public class CardTriggerEffectFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CardTriggerEffectFinalizer> logger;
        private readonly IRegister<CardEffectData> effectRegister;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IRegister<CardTriggerType> triggerEnumRegister;
        private readonly ICache<IDefinition<CardTriggerEffectData>> cache;
        private readonly PluginAtlas atlas;

        public CardTriggerEffectFinalizer(
            PluginAtlas atlas,
            IModLogger<CardTriggerEffectFinalizer> logger,
            IRegister<CardEffectData> effectRegister,
            IRegister<CardUpgradeData> upgradeRegister,
            IRegister<CardTriggerType> triggerEnumRegister,
            ICache<IDefinition<CardTriggerEffectData>> cache
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.effectRegister = effectRegister;
            this.upgradeRegister = upgradeRegister;
            this.triggerEnumRegister = triggerEnumRegister;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeTrigger(definition);
            }
            cache.Clear();
        }

        private void FinalizeTrigger(IDefinition<CardTriggerEffectData> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var data = definition.Data;

            logger.Log(LogLevel.Debug, 
                $"Finalizing Card Trigger {key.GetId(TemplateConstants.CardTrigger, definition.Id)}... "
            );

            //handle trigger
            var trigger = CardTriggerType.OnCast;
            var triggerReference = configuration.GetSection("trigger").ParseReference();
            if (triggerReference != null)
            {
                if (
                    triggerEnumRegister.TryLookupId(
                        triggerReference.ToId(key, TemplateConstants.CardTriggerEnum),
                        out var triggerFound,
                        out var _
                    )
                )
                {
                    trigger = triggerFound;
                }
            }
            AccessTools
                .Field(typeof(CardTriggerEffectData), "trigger")
                .SetValue(data, trigger);


            var triggers = new List<CardTriggerData>();
            foreach (var child in configuration.GetSection("trigger_effects").GetChildren())
            {
                var triggerData = new CardTriggerData();

                triggerData.persistenceMode =
                    child.GetDeprecatedSection("persistence", "persistence_mode").ParsePersistenceMode()
                    ?? PersistenceMode.SingleRun;
                triggerData.paramInt = child.GetSection("param_int").ParseInt() ?? 0;

                var effectStateReference = child.GetSection("trigger_effect").ParseReference();
                if (effectStateReference == null)
                {
                    continue;
                }
                var triggerEffectName = effectStateReference.id;
                var modReference = effectStateReference.mod_reference ?? key;
                var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
                if (
                    !triggerEffectName.GetFullyQualifiedName<ICardTriggerEffect>(
                        assembly,
                        out string? fullyQualifiedName
                    )
                )
                {
                    logger.Log(LogLevel.Error, $"Failed to load trigger effect state name {triggerEffectName} in {definition.Id} with mod reference {modReference}. Make sure the class implements interface ICardTriggerEffect.");
                    continue;
                }
                triggerData.cardTriggerEffect = fullyQualifiedName;

                effectStateReference = child.GetSection("buff_effect").ParseReference();
                if (effectStateReference == null)
                {
                    continue;
                }
                var effectStateName = effectStateReference.id;
                modReference = effectStateReference.mod_reference ?? key;
                assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
                if (
                    !triggerEffectName.GetFullyQualifiedName<CardEffectBase>(
                        assembly,
                        out fullyQualifiedName
                    )
                )
                {
                    logger.Log(LogLevel.Error, $"Failed to load effect state name {effectStateName} in {definition.Id} with mod reference {modReference}. Make sure the class inherits from CardEffectBase.");
                    continue;
                }
                triggerData.buffEffectType = fullyQualifiedName;

                var upgradeReference = child.GetSection("param_upgrade").ParseReference();
                if (upgradeReference != null)
                {
                    upgradeRegister.TryLookupName(
                        upgradeReference.ToId(key, TemplateConstants.Upgrade),
                        out var lookup,
                        out var _
                    );
                    triggerData.paramUpgrade = lookup;
                }

                triggers.Add(triggerData);
            }
            AccessTools
                .Field(typeof(CardTriggerEffectData), "cardTriggerEffects")
                .SetValue(data, triggers);

            var effects = new List<CardEffectData>();
            var effectReferences = configuration.GetSection("effects")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in effectReferences)
            {
                var id = reference.ToId(key, TemplateConstants.Effect);
                if (effectRegister.TryLookupId(id, out var lookup, out var _))
                {
                    effects.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardTriggerEffectData), "cardEffects").SetValue(data, effects);
        }
    }
}
