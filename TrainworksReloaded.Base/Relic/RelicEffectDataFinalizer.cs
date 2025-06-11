using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TrainworksReloaded.Core.Interfaces;
using TrainworksReloaded.Base.Extensions;
using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Extensions;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<RelicEffectDataFinalizer> logger;
        private readonly ICache<IDefinition<RelicEffectData>> cache;
        private readonly IRegister<AdditionalTooltipData> tooltipRegister;
        private readonly IRegister<CardEffectData> cardEffectRegister;
        private readonly IRegister<CardPool> cardPoolRegister;
        private readonly IRegister<CharacterData> characterRegister;
        private readonly IRegister<StatusEffectData> statusEffectRegister;
        private readonly IRegister<CardTraitData> traitRegister;
        private readonly IRegister<CharacterTriggerData> triggerRegister;
        private readonly IRegister<CardData> cardRegister;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IRegister<RewardData> rewardRegister;
        private readonly IRegister<CardUpgradeMaskData> cardUpgradeMaskRegister;
        private readonly IRegister<SubtypeData> subtypeRegister;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;
        private readonly IRegister<EnhancerPool> enhancerPoolRegister;
        private readonly IRegister<RelicEffectCondition> relicEffectConditionRegister;

        public RelicEffectDataFinalizer(
            IModLogger<RelicEffectDataFinalizer> logger,
            ICache<IDefinition<RelicEffectData>> cache,
            IRegister<AdditionalTooltipData> tooltipRegister,
            IRegister<CardEffectData> cardEffectRegister,
            IRegister<CardPool> cardPoolRegister,
            IRegister<CharacterData> characterRegister,
            IRegister<CardTraitData> traitRegister,
            IRegister<CharacterTriggerData> triggerRegister,
            IRegister<CardData> cardRegister,
            IRegister<CardUpgradeData> upgradeRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<RewardData> rewardRegister,
            IRegister<SubtypeData> subtypeRegister,
            IRegister<CardUpgradeMaskData> cardUpgradeMaskRegister,
            IRegister<VfxAtLoc> vfxRegister,
            IRegister<RelicData> relicRegister,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister,
            IRegister<EnhancerPool> enhancerPoolRegister,
            IRegister<RelicEffectCondition> relicEffectConditionRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.tooltipRegister = tooltipRegister;
            this.cardEffectRegister = cardEffectRegister;
            this.cardPoolRegister = cardPoolRegister;
            this.characterRegister = characterRegister;
            this.traitRegister = traitRegister;
            this.triggerRegister = triggerRegister;
            this.cardRegister = cardRegister;
            this.upgradeRegister = upgradeRegister;
            this.statusEffectRegister = statusRegister;
            this.rewardRegister = rewardRegister;
            this.cardUpgradeMaskRegister = cardUpgradeMaskRegister;
            this.subtypeRegister = subtypeRegister;
            this.vfxRegister = vfxRegister;
            this.relicRegister = relicRegister;
            this.triggerEnumRegister = triggerEnumRegister;
            this.enhancerPoolRegister = enhancerPoolRegister;
            this.relicEffectConditionRegister = relicEffectConditionRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRelicEffectData(definition);
            }
            cache.Clear();
        }

        private void FinalizeRelicEffectData(IDefinition<RelicEffectData> definition)
        {
            /*
            Types that need registers but don't have them yet:
            - CharacterSubstitution
            - RelicEffectCondition
            - RarityTicketMultiplier
            - MerchantData
            - GrantableRewardData
            - CardSetBuilder
            - CollectibleRelicData
            - CovenantData
            - RandomChampionPool
            - RoomData
            */ 
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Debug, $"Finalizing RelicEffect {key} {definition.Id}... ");

            // Handle status effects
            var statusEffects = new List<StatusEffectStackData>();
            var statusEffectsConfig = configuration.GetSection("param_status_effects").GetChildren();
            foreach (var child in statusEffectsConfig)
            {
                var statusReference = child.GetSection("status").ParseReference();
                if (statusReference == null)
                {
                    continue;
                }
                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusEffectRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    statusEffects.Add(new StatusEffectStackData()
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child?.GetSection("count").ParseInt() ?? 0,
                    });
                }
            }
            AccessTools
                .Field(typeof(RelicEffectData), "paramStatusEffects")
                .SetValue(data, statusEffects.ToArray());

            // Handle card effects
            var cardEffects = new List<CardEffectData>();
            var cardEffectsReferences = configuration.GetDeprecatedSection("param_card_effects", "param_effects")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in cardEffectsReferences)
            {
                
                var id = reference.ToId(key, TemplateConstants.Effect);
                if (cardEffectRegister.TryLookupId(id, out var cardEffect, out var _))
                {
                    cardEffects.Add(cardEffect);
                }
            }
            if (cardEffects.Count != 0)
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardEffects").SetValue(data, cardEffects);
            }

            // Handle card pool
            var cardPoolReference = configuration.GetSection("param_card_pool").ParseReference();
            if (cardPoolReference != null && cardPoolRegister.TryLookupId(cardPoolReference.ToId(key, TemplateConstants.CardPool), out var cardPool, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardPool").SetValue(data, cardPool);
            }

            // Handle characters
            var characters = new List<CharacterData>();
            var characterReferences = configuration.GetSection("param_characters")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in characterReferences)
            {
                var id = reference.ToId(key, TemplateConstants.Character);
                if (characterRegister.TryLookupName(id, out var character, out var _))
                {
                    characters.Add(character);
                }
            }
            AccessTools.Field(typeof(RelicEffectData), "paramCharacters").SetValue(data, characters);

            // Handle traits
            var traits = new List<CardTraitData>();
            var traitsReferences = configuration.GetSection("traits")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in traitsReferences)
            {
                var id = reference.ToId(key, TemplateConstants.Trait);
                if (traitRegister.TryLookupId(id, out var trait, out var _))
                {
                    traits.Add(trait);
                }
            }
            if (traits.Count > 0)
            {
                AccessTools.Field(typeof(RelicEffectData), "traits").SetValue(data, traits);
            }

            // Handle excluded traits
            var excludedTraits = new List<CardTraitData>();
            var excludedTraitsReference = configuration.GetSection("excluded_traits")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in excludedTraitsReference)
            {
                var id = reference.ToId(key, TemplateConstants.Trait);
                if (traitRegister.TryLookupId(id, out var trait, out var _))
                {
                    excludedTraits.Add(trait);
                }
            }
            if (excludedTraits.Count > 0)
            {
                AccessTools.Field(typeof(RelicEffectData), "excludedTraits").SetValue(data, excludedTraits);
            }

            // Handle triggers
            var triggers = new List<CharacterTriggerData>();
            var triggerReferences = configuration.GetSection("triggers")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in triggerReferences)
            {   
                var id = reference.ToId(key, TemplateConstants.CharacterTrigger);
                if (triggerRegister.TryLookupId(id, out var trigger, out var _))
                {
                    triggers.Add(trigger);
                }
            }
            if (triggers.Count != 0)
            {
                AccessTools.Field(typeof(RelicEffectData), "triggers").SetValue(data, triggers);
            }

            // Handle card data
            var cardReference = configuration.GetDeprecatedSection("param_card_data", "param_card").ParseReference();
            if (cardReference != null && cardRegister.TryLookupId(cardReference.ToId(key, TemplateConstants.Card), out var cardData, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardData").SetValue(data, cardData);
            }

            // Handle card upgrade data
            var upgradeReference = configuration.GetSection("param_upgrade").ParseReference();
            if (upgradeReference != null && upgradeRegister.TryLookupName(upgradeReference.ToId(key, TemplateConstants.Upgrade), out var cardUpgradeData, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardUpgradeData").SetValue(data, cardUpgradeData);
            }

            //handle paramReward 
            var rewardReference = configuration.GetSection("param_reward").ParseReference();
            if (rewardReference != null && rewardRegister.TryLookupId(rewardReference.ToId(key, TemplateConstants.RewardData), out var reward, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramReward").SetValue(data, reward);
            }

            //handle paramReward 2
            var rewardReference2 = configuration.GetSection("param_reward_2").ParseReference();
            if (rewardReference2 != null && rewardRegister.TryLookupId(rewardReference2.ToId(key, TemplateConstants.RewardData), out var reward2, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramReward2").SetValue(data, reward2);
            }

            //handle paramCardFilter
            var paramCardFilter = configuration.GetSection("param_card_filter").ParseReference();
            if (paramCardFilter != null && cardUpgradeMaskRegister.TryLookupId(paramCardFilter.ToId(key, TemplateConstants.UpgradeMask), out var cardFilter, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardFilter").SetValue(data, cardFilter);
            }
            
            //handle paramCardFilterSecondary
            var paramCardFilterSecondary = configuration.GetDeprecatedSection("param_card_filter_secondary", "param_card_filter_2").ParseReference();
            if (paramCardFilterSecondary != null && cardUpgradeMaskRegister.TryLookupId(paramCardFilterSecondary.ToId(key, TemplateConstants.UpgradeMask), out var cardFilterSecondary, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardFilterSecondary").SetValue(data, cardFilterSecondary);
            }

            // Handle character subtype
            var characterSubtype = "SubtypesData_None";
            var characterSubtypeReference = configuration.GetDeprecatedSection("character_subtype", "param_subtype").ParseReference();
            if (characterSubtypeReference != null)
            {
                if (subtypeRegister.TryLookupId(
                    characterSubtypeReference.ToId(key, TemplateConstants.Subtype),
                    out var lookup,
                    out var _))
                {
                    characterSubtype = lookup.Key;
                }
            }
            AccessTools.Field(typeof(RelicEffectData), "paramCharacterSubtype").SetValue(data, characterSubtype);

            // Handle excluded character subtypes
            List<string> excludedSubtypes = [];
            var subtypeReferences = configuration.GetDeprecatedSection("excluded_character_subtypes", "param_excluded_subtypes")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in subtypeReferences)
            {
                if (subtypeRegister.TryLookupId(
                    reference.ToId(key, TemplateConstants.Subtype),
                    out var lookup,
                    out var _))
                {
                    excludedSubtypes.Add(lookup.Key);
                }
               
            }
            AccessTools.Field(typeof(RelicEffectData), "paramExcludeCharacterSubtypes").SetValue(data, excludedSubtypes.ToArray());

            var appliedVFXId = configuration.GetSection("applied_vfx").ParseReference()?.ToId(key, TemplateConstants.Vfx) ?? "";
            if (vfxRegister.TryLookupId(appliedVFXId, out var appliedVFX, out var _))
            {
                AccessTools.Field(typeof(RelicEffectData), "appliedVfx").SetValue(data, appliedVFX);
            }

            var relicReference = configuration.GetSection("param_relic").ParseReference();
            if (relicReference != null &&
                relicRegister.TryLookupId(
                    relicReference.ToId(key, TemplateConstants.RelicData),
                    out var relic,
                    out var _
                )
            )
            {
                AccessTools.Field(typeof(RelicEffectData), "paramRelic").SetValue(data, relic as CollectableRelicData);
            }

            var paramTrigger = CharacterTriggerData.Trigger.OnDeath;
            var triggerReference = configuration.GetSection("param_trigger").ParseReference();
            if (triggerReference != null)
            {
                if (
                    triggerEnumRegister.TryLookupId(
                        triggerReference.ToId(key, TemplateConstants.CharacterTriggerEnum),
                        out var triggerFound,
                        out var _
                    )
                )
                {
                    paramTrigger = triggerFound;
                }
            }
            AccessTools
                .Field(typeof(RelicEffectData), "paramTrigger")
                .SetValue(data, paramTrigger);

            var enhancerPoolReference = configuration.GetSection("param_enhancer_pool").ParseReference();
            if (enhancerPoolReference != null)
            {
                if (
                    enhancerPoolRegister.TryLookupId(
                        enhancerPoolReference.ToId(key, TemplateConstants.EnhancerPool),
                        out var enhancerPool,
                        out var _
                    )
                )
                {
                    AccessTools
                        .Field(typeof(RelicEffectData), "paramEnhancerPool")
                        .SetValue(data, enhancerPool);
                }
            }

            var tooltips = new List<AdditionalTooltipData>();
            var tooltipReferences = configuration
                .GetSection("additional_tooltips")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in tooltipReferences)
            {
                var id = reference.ToId(key, TemplateConstants.AdditionalTooltip);
                if (tooltipRegister.TryLookupName(id, out var tooltip, out var _))
                {
                    tooltips.Add(tooltip);
                }
            }
            AccessTools
                .Field(typeof(RelicEffectData), "additionalTooltips")
                .SetValue(data, tooltips.ToArray());

            var conditions = new List<RelicEffectCondition>();
            var conditionReferences = configuration
                .GetSection("conditions")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in conditionReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RelicEffectCondition);
                if (relicEffectConditionRegister.TryLookupName(id, out var lookup, out var _))
                {
                    conditions.Add(lookup);
                }
            }
            AccessTools.Field(typeof(RelicEffectData), "effectConditions").SetValue(data, conditions);
        }
    }
}
