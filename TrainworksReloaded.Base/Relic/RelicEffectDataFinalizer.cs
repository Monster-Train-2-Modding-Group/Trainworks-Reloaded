using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Enums;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
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
        private readonly IRegister<TargetMode> targetModeRegister;

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
            IRegister<RelicEffectCondition> relicEffectConditionRegister,
            IRegister<TargetMode> targetModeRegister
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
            this.targetModeRegister = targetModeRegister;
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

            logger.Log(LogLevel.Info, $"Finalizing RelicEffect {key} {definition.Id} path: {configuration.GetPath()}...");

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
                if (statusEffectRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, statusReference.context))
                {
                    statusEffects.Add(new StatusEffectStackData()
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child?.GetSection("count").ParseInt() ?? 0,
                        fromPermanentUpgrade = child?.GetSection("from_permanent_upgrade").ParseBool() ?? false
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
                if (cardEffectRegister.TryLookupId(id, out var cardEffect, out var _, reference.context))
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
            if (cardPoolReference != null && cardPoolRegister.TryLookupId(cardPoolReference.ToId(key, TemplateConstants.CardPool), out var cardPool, out var _, cardPoolReference.context))
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
                if (characterRegister.TryLookupName(id, out var character, out var _, reference.context))
                {
                    characters.Add(character);
                }
            }
            AccessTools.Field(typeof(RelicEffectData), "paramCharacters").SetValue(data, characters);

            var entries = new List<CharacterSubstitution.Entry>();
            foreach (var item in configuration.GetSection("param_character_substitution").GetChildren())
            {
                var entry = ParseCharacterSubstitutionEntry(key, item);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }
            if (entries.Count > 0)
            {
                var characterSubstitution = ScriptableObject.CreateInstance<CharacterSubstitution>();
                AccessTools.Field(typeof(CharacterSubstitution), "entries").SetValue(characterSubstitution, entries);
                AccessTools.Field(typeof(RelicEffectData), "paramCharacterSubstitution").SetValue(data, characterSubstitution);
            }

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
                if (traitRegister.TryLookupId(id, out var trait, out var _, reference.context))
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
                if (traitRegister.TryLookupId(id, out var trait, out var _, reference.context))
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
                if (triggerRegister.TryLookupId(id, out var trigger, out var _, reference.context))
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
            if (cardReference != null && cardRegister.TryLookupId(cardReference.ToId(key, TemplateConstants.Card), out var cardData, out var _, cardReference.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardData").SetValue(data, cardData);
            }

            // Handle card upgrade data
            var upgradeReference = configuration.GetSection("param_upgrade").ParseReference();
            if (upgradeReference != null && upgradeRegister.TryLookupName(upgradeReference.ToId(key, TemplateConstants.Upgrade), out var cardUpgradeData, out var _, upgradeReference.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardUpgradeData").SetValue(data, cardUpgradeData);
            }

            //handle paramReward 
            var rewardReference = configuration.GetSection("param_reward").ParseReference();
            if (rewardReference != null && rewardRegister.TryLookupName(rewardReference.ToId(key, TemplateConstants.RewardData), out var reward, out var _, rewardReference.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramReward").SetValue(data, reward);
            }

            //handle paramReward 2
            var rewardReference2 = configuration.GetSection("param_reward_2").ParseReference();
            if (rewardReference2 != null && rewardRegister.TryLookupName(rewardReference2.ToId(key, TemplateConstants.RewardData), out var reward2, out var _, rewardReference2.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramReward2").SetValue(data, reward2);
            }

            var rewardReference3 = configuration.GetSection("param_grantable_reward").ParseReference();
            if (rewardReference3 != null && rewardRegister.TryLookupName(rewardReference3.ToId(key, TemplateConstants.RewardData), out var reward3, out var _, rewardReference3.context))
            {
                if (reward3 is not GrantableRewardData grantableReward)
                {
                    logger.Log(LogLevel.Warning, $"RelicEffectData {definition.Id} Attempted to add a non-GrantableRewardData RewardData {reward3.name}. Ignoring...");
                }
                else
                {
                    AccessTools.Field(typeof(RelicEffectData), "paramGrantableReward").SetValue(data, grantableReward);
                }
            }

            //handle paramCardFilter
            var paramCardFilter = configuration.GetSection("param_card_filter").ParseReference();
            if (paramCardFilter != null && cardUpgradeMaskRegister.TryLookupId(paramCardFilter.ToId(key, TemplateConstants.UpgradeMask), out var cardFilter, out var _, paramCardFilter.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardFilter").SetValue(data, cardFilter);
            }

            //handle paramCardFilterSecondary
            var paramCardFilterSecondary = configuration.GetDeprecatedSection("param_card_filter_secondary", "param_card_filter_2").ParseReference();
            if (paramCardFilterSecondary != null && cardUpgradeMaskRegister.TryLookupId(paramCardFilterSecondary.ToId(key, TemplateConstants.UpgradeMask), out var cardFilterSecondary, out var _, paramCardFilterSecondary.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardFilterSecondary").SetValue(data, cardFilterSecondary);
            }

            var paramCardFilter3 = configuration.GetSection("param_card_upgrade_mask_data").ParseReference();
            if (paramCardFilter3 != null && cardUpgradeMaskRegister.TryLookupId(paramCardFilter3.ToId(key, TemplateConstants.UpgradeMask), out var cardFilter3, out var _, paramCardFilter3.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "paramCardUpgradeMaskData").SetValue(data, cardFilter3);
            }

            // Handle character subtype
            var characterSubtype = "SubtypesData_None";
            var characterSubtypeReference = configuration.GetDeprecatedSection("character_subtype", "param_subtype").ParseReference();
            if (characterSubtypeReference != null)
            {
                if (subtypeRegister.TryLookupId(
                    characterSubtypeReference.ToId(key, TemplateConstants.Subtype),
                    out var lookup,
                    out var _, characterSubtypeReference.context))
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
                    out var _, reference.context))
                {
                    excludedSubtypes.Add(lookup.Key);
                }

            }
            AccessTools.Field(typeof(RelicEffectData), "paramExcludeCharacterSubtypes").SetValue(data, excludedSubtypes.ToArray());

            
            var upgradeReferences = configuration.GetSection("param_card_upgrade_pool")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            if (!upgradeReferences.IsNullOrEmpty())
            {
                List<CardUpgradeData> upgrades = [];
                foreach (var reference in upgradeReferences)
                {
                    if (reference != null && upgradeRegister.TryLookupName(reference.ToId(key, TemplateConstants.Upgrade), out var item, out var _, reference.context))
                    {
                        upgrades.Add(item);
                    }
                }
                var pool = ScriptableObject.CreateInstance<CardUpgradePool>();
                AccessTools.Field(typeof(CardUpgradePool), "cardUpgradeDataList").SetValue(pool, upgrades);
                AccessTools.Field(typeof(RelicEffectData), "paramCardUpgradePool").SetValue(data, pool);
            }

            var appliedVFXReference = configuration.GetSection("applied_vfx").ParseReference();
            if (vfxRegister.TryLookupId(appliedVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var appliedVFX, out var _, appliedVFXReference?.context))
            {
                AccessTools.Field(typeof(RelicEffectData), "appliedVfx").SetValue(data, appliedVFX);
            }

            var relicReference = configuration.GetSection("param_relic").ParseReference();
            if (relicReference != null &&
                relicRegister.TryLookupName(
                    relicReference.ToId(key, TemplateConstants.RelicData),
                    out var relic,
                    out var _,
                    relicReference.context
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
                        out var _,
                        triggerReference.context
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
                        out var _,
                        enhancerPoolReference.context
                    )
                )
                {
                    AccessTools
                        .Field(typeof(RelicEffectData), "paramEnhancerPool")
                        .SetValue(data, enhancerPool);
                }
            }

            var targetModeReference = configuration.GetSection("target_mode").ParseReference();
            if (targetModeReference != null)
            {
                targetModeRegister.TryLookupId(
                    targetModeReference.ToId(key, TemplateConstants.TargetModeEnum),
                    out var lookup,
                    out var _,
                    targetModeReference.context);
                AccessTools.Field(typeof(RelicEffectData), "paramTargetMode").SetValue(data, lookup);
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
                if (tooltipRegister.TryLookupName(id, out var tooltip, out var _, reference.context))
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
                if (relicEffectConditionRegister.TryLookupName(id, out var lookup, out var _, reference.context))
                {
                    conditions.Add(lookup);
                }
            }
            AccessTools.Field(typeof(RelicEffectData), "effectConditions").SetValue(data, conditions);

            List<RandomChampionPool.GrantedChampionInfo> champions = [];
            foreach (var item in configuration.GetSection("param_random_champion_pool").GetChildren())
            {
                var champ = ParseGrantedChampionInfo(key, item);
                if (champ != null)
                {
                    champions.Add(champ);
                }
            }
            if (champions.Count > 0)
            {
                var championPool = ScriptableObject.CreateInstance<RandomChampionPool>();
                var championPoolData = AccessTools.Field(typeof(RandomChampionPool), "championInfoList").GetValue(championPool) as ReorderableArray<RandomChampionPool.GrantedChampionInfo>;
                championPoolData!.CopyFrom(champions);
                AccessTools.Field(typeof(RelicEffectData), "paramRandomChampionPool").SetValue(data, championPool);
            }

            List<CardPull> cardPulls = [];
            foreach (var item in configuration.GetSection("param_card_set_builder").GetChildren())
            {
                var cardPull = ParseCardPull(key, item);
                if (cardPull != null)
                {
                    cardPulls.Add(cardPull);
                }
            }
            if (cardPulls.Count > 0)
            {
                var cardSetBuilder = ScriptableObject.CreateInstance<CardSetBuilder>();
                AccessTools.Field(typeof(CardSetBuilder), "paramCardPulls").SetValue(cardSetBuilder, cardPulls);
                AccessTools.Field(typeof(RelicEffectData), "paramCardSetBuilder").SetValue(data, cardSetBuilder);
            }
        }

        RandomChampionPool.GrantedChampionInfo? ParseGrantedChampionInfo(string key, IConfigurationSection configuration)
        {
            if (!configuration.Exists())
                return null;

            var cardReference = configuration.GetSection("champion_card").ParseReference();
            CardData? card = null;
            if (cardReference != null && cardRegister.TryLookupName(cardReference.ToId(key, TemplateConstants.Card), out var lookup, out var _, cardReference.context))
            {
                card = lookup;
            }

            if (card == null)
                return null;

            List<CardUpgradeData> upgrades = [];
            var upgradeReferences = configuration
                .GetSection("upgrades")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in upgradeReferences)
            {
                if (upgradeRegister.TryLookupName(reference.ToId(key, TemplateConstants.Upgrade), out var upgradeLookup, out var _, reference.context))
                {
                    upgrades.Add(upgradeLookup);
                }
            }

            return new RandomChampionPool.GrantedChampionInfo { 
                championCard = card,
                upgrades = upgrades,
            };
        }

        CharacterSubstitution.Entry? ParseCharacterSubstitutionEntry(string key, IConfigurationSection configuration)
        {
            if (!configuration.Exists())
                return null;

            var characterReference = configuration.GetSection("unit_before").ParseReference();
            CharacterData? character = null;
            if (characterReference != null && characterRegister.TryLookupName(characterReference.ToId(key, TemplateConstants.Character), out var lookup, out var _, characterReference.context))
            {
                character = lookup;
            }

            var characterReference2 = configuration.GetSection("unit_after").ParseReference();
            CharacterData? character2 = null;
            if (characterReference2 != null && characterRegister.TryLookupName(characterReference2.ToId(key, TemplateConstants.Character), out lookup, out var _, characterReference2.context))
            {
                character2 = lookup;
            }

            if (character == null || character2 == null)
                return null;

            return new CharacterSubstitution.Entry
            {
                UnitBefore = character,
                UnitAfter = character2,
            };
        }

        CardPull? ParseCardPull(string key, IConfigurationSection configuration)
        {
            var data = ScriptableObject.CreateInstance<CardPull>();

            var cardPoolReference = configuration.GetSection("card_pool").ParseReference();
            CardPool? cardPool = null;
            if (cardPoolReference != null && cardPoolRegister.TryLookupName(cardPoolReference.ToId(key, TemplateConstants.CardPool), out var lookup, out var _, cardPoolReference.context))
            {
                cardPool = lookup;
            }
            AccessTools.Field(typeof(CardPull), "cardPool").SetValue(data, cardPool);

            var classType = configuration.GetSection("class_type").ParseClassType() ?? RunState.ClassType.MainClass;
            AccessTools.Field(typeof(CardPull), "classType").SetValue(data, classType);

            var copies = configuration.GetSection("num_copies").ParseInt() ?? 1;
            if (copies <= 0)
                copies = 1;
            AccessTools.Field(typeof(CardPull), "numCopies").SetValue(data, (uint)copies);

            var hasRarityCondition = configuration.GetSection("has_rarity_condition").ParseBool() ?? false;
            AccessTools.Field(typeof(CardPull), "hasRarityCondition").SetValue(data, hasRarityCondition);

            var rarity = configuration.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common;
            AccessTools.Field(typeof(CardPull), "cardRarity").SetValue(data, rarity);

            var showcaseAtRunStart = configuration.GetSection("showcase_at_run_start").ParseBool() ?? false;
            AccessTools.Field(typeof(CardPull), "showcaseAtRunStart").SetValue(data, showcaseAtRunStart);

            return data;
        }
    }
}
