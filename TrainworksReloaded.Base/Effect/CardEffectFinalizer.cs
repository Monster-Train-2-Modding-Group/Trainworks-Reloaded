using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Effect
{
    public class CardEffectFinalizer : IDataFinalizer
    {
        private readonly IRegister<AdditionalTooltipData> tooltipRegister;
        private readonly IRegister<CardData> cardRegister;
        private readonly IRegister<CharacterData> characterDataRegister;
        private readonly IRegister<CardUpgradeData> cardUpgradeRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;
        private readonly IRegister<CardUpgradeMaskData> upgradeMaskRegister;
        private readonly IRegister<CardPool> cardPoolRegister;
        private readonly IRegister<SubtypeData> subtypeRegister;
        private readonly IRegister<TargetMode> targetModeRegister;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<RelicData> relicRegister;
        private readonly ICache<IDefinition<CardEffectData>> cache;

        public CardEffectFinalizer(
            IRegister<AdditionalTooltipData> tooltipRegister,
            IRegister<CardData> cardRegister,
            IRegister<CharacterData> characterDataRegister,
            IRegister<CardUpgradeData> cardUpgradeRegister,
            IRegister<CardUpgradeMaskData> upgradeMaskRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister,
            IRegister<CardPool> cardPoolRegister,
            IRegister<SubtypeData> subtypeRegister,
            IRegister<TargetMode> targetModeRegister,
            IRegister<VfxAtLoc> vfxRegister,
            IRegister<RelicData> relicRegister,
            ICache<IDefinition<CardEffectData>> cache
        )
        {
            this.tooltipRegister = tooltipRegister;
            this.cardRegister = cardRegister;
            this.characterDataRegister = characterDataRegister;
            this.cardUpgradeRegister = cardUpgradeRegister;
            this.statusRegister = statusRegister;
            this.triggerEnumRegister = triggerEnumRegister;
            this.upgradeMaskRegister = upgradeMaskRegister;
            this.cardPoolRegister = cardPoolRegister;
            this.subtypeRegister = subtypeRegister;
            this.targetModeRegister = targetModeRegister;
            this.vfxRegister = vfxRegister;
            this.relicRegister = relicRegister;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCardEffectData(definition);
            }
            cache.Clear();
        }

        private void FinalizeCardEffectData(IDefinition<CardEffectData> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var data = definition.Data;

            var cardReference = configuration.GetDeprecatedSection("param_card_data", "param_card").ParseReference();
            if (
                cardReference != null
                && cardRegister.TryLookupName(
                    cardReference.ToId(key, TemplateConstants.Card),
                    out var cardData,
                    out var _,
                    cardReference.context
                )
            )
            {
                AccessTools
                    .Field(typeof(CardEffectData), "paramCardData")
                    .SetValue(data, cardData);
            }

            var characterReference = configuration.GetDeprecatedSection("param_character_data", "param_character").ParseReference();
            if (
                characterReference != null
                && characterDataRegister.TryLookupName(
                    characterReference.ToId(key, TemplateConstants.Character),
                    out var characterData,
                    out var _,
                    characterReference.context
                )
            )
            {
                AccessTools
                    .Field(typeof(CardEffectData), "paramCharacterData")
                    .SetValue(data, characterData);
            }

            var characterReference2 = configuration.GetDeprecatedSection("param_character_data_2", "param_character_2").ParseReference();
            if (
                characterReference2 != null
                && characterDataRegister.TryLookupName(
                    characterReference2.ToId(key, TemplateConstants.Character),
                    out var characterData2,
                    out var _,
                    characterReference2.context
                )
            )
            {
                AccessTools
                    .Field(typeof(CardEffectData), "paramAdditionalCharacterData")
                    .SetValue(data, characterData2);
            }

            //card pools
            var characterDataPool = new List<CharacterData>();
            var characterReferences = configuration
                .GetDeprecatedSection("param_character_data_pool", "param_character_pool")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var characterPoolReference in characterReferences)
            {
                if (
                    characterDataRegister.TryLookupName(
                        characterPoolReference.ToId(key, TemplateConstants.Character),
                        out var character,
                        out var _,
                        characterPoolReference.context
                    )
                )
                {
                    characterDataPool.Add(character);
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "paramCharacterDataPool")
                .SetValue(data, characterDataPool);

            //upgrades
            var upgradeReference = configuration.GetSection("param_upgrade").ParseReference();
            if (
                upgradeReference != null
                && cardUpgradeRegister.TryLookupName(
                    upgradeReference.ToId(key, TemplateConstants.Upgrade),
                    out var upgradeData,
                    out var _,
                    upgradeReference.context
                )
            )
            {
                AccessTools
                    .Field(typeof(CardEffectData), "paramCardUpgradeData")
                    .SetValue(data, upgradeData);
            }

            // Status effects.
            string statusEffectStackMultiplier = string.Empty;
            var statusEffectStackMultiplierReference = configuration.GetSection("status_effect_multiplier").ParseReference();
            if (statusEffectStackMultiplierReference != null)
            {
                var statusEffectId = statusEffectStackMultiplierReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, statusEffectStackMultiplierReference.context))
                {
                    statusEffectStackMultiplier = statusEffectData.GetStatusId();
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "statusEffectStackMultiplier")
                .SetValue(data, statusEffectStackMultiplier);


            //string[]
            var targetModeStatusEffectsFilterReferences = configuration.GetDeprecatedSection("status_effect_filters", "target_mode_status_effect_filter")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            List<string> targetModeStatusEffectsFilter = [];
            foreach (var statusReference in targetModeStatusEffectsFilterReferences)
            {
                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, statusReference.context))
                {
                    targetModeStatusEffectsFilter.Add(statusEffectData.GetStatusId());
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "targetModeStatusEffectsFilter")
                .SetValue(data, targetModeStatusEffectsFilter.ToArray());

            List<StatusEffectStackData> paramStatusEffects = [];
            foreach (var child in configuration.GetSection("param_status_effects").GetChildren())
            {
                var statusReference = child.GetSection("status").ParseReference();
                if (statusReference == null)
                    continue;
                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, statusReference.context))
                {
                    paramStatusEffects.Add(new StatusEffectStackData
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child?.GetSection("count").ParseInt() ?? 0,
                        fromPermanentUpgrade = child?.GetSection("from_permanent_upgrade").ParseBool() ?? false
                    });
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "paramStatusEffects")
                .SetValue(data, paramStatusEffects.ToArray());

            //trigger
            var paramTrigger = CharacterTriggerData.Trigger.OnDeath;
            var triggerReference = configuration.GetDeprecatedSection("trigger", "param_trigger").ParseReference();
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
                .Field(typeof(CardEffectData), "paramTrigger")
                .SetValue(data, paramTrigger);

            var filterReference = configuration.GetSection("param_card_filter").ParseReference();
            if (filterReference != null)
            {
                upgradeMaskRegister.TryLookupId(
                    filterReference.ToId(key, TemplateConstants.UpgradeMask),
                    out var lookup,
                    out var _, filterReference.context);
                AccessTools.Field(typeof(CardEffectData), "paramCardFilter").SetValue(data, lookup);
            }

            var filter2Reference = configuration.GetSection("param_card_filter_2").ParseReference();
            if (filter2Reference != null)
            {
                upgradeMaskRegister.TryLookupId(
                    filter2Reference.ToId(key, TemplateConstants.UpgradeMask),
                    out var lookup,
                    out var _, filter2Reference.context);
                AccessTools.Field(typeof(CardEffectData), "paramCardFilterSecondary").SetValue(data, lookup);
            }

            var paramRelicReference = configuration.GetSection("param_relic").ParseReference();
            if (paramRelicReference != null)
            {
                relicRegister.TryLookupName(
                    paramRelicReference.ToId(key, TemplateConstants.RelicData),
                    out var lookup,
                    out var _, paramRelicReference.context);
                AccessTools.Field(typeof(CardEffectData), "paramRelicData").SetValue(data, lookup);
            }

            var poolReference = configuration.GetSection("param_card_pool").ParseReference();
            if (poolReference != null)
            {
                cardPoolRegister.TryLookupId(
                    poolReference.ToId(key, TemplateConstants.CardPool),
                    out var lookup,
                    out var _,
                    poolReference.context);
                AccessTools.Field(typeof(CardEffectData), "paramCardPool").SetValue(data, lookup);
            }

            var targetModeReference = configuration.GetSection("target_mode").ParseReference();
            if (targetModeReference != null) 
            {
                targetModeRegister.TryLookupId(
                    targetModeReference.ToId(key, TemplateConstants.TargetModeEnum),
                    out var lookup,
                    out var _,
                    targetModeReference.context);
                AccessTools.Field(typeof(CardEffectData), "targetMode").SetValue(data, lookup);
            }

            var targetCharacterSubtype = "SubtypesData_None";
            var targetCharacterSubtypeReference = configuration.GetSection("target_subtype").ParseReference();
            if (targetCharacterSubtypeReference != null)
            {
                if (subtypeRegister.TryLookupId(
                    targetCharacterSubtypeReference.ToId(key, TemplateConstants.Subtype),
                    out var lookup,
                    out var _,
                    targetCharacterSubtypeReference.context))
                {
                    targetCharacterSubtype = lookup.Key;
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "targetCharacterSubtype")
                .SetValue(data, targetCharacterSubtype);

            var excludedSubtypeReferences = configuration.GetSection("target_character_excluded_subtypes")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            List<string> excludedSubtypes = [];
            foreach (var subtypeReference in excludedSubtypeReferences)
            {
                var subtypeId = subtypeReference.ToId(key, TemplateConstants.Subtype);
                if (subtypeRegister.TryLookupId(subtypeId, out var subtype, out var _, subtypeReference.context))
                {
                    excludedSubtypes.Add(subtype.Key);
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "targetCharacterExcludedSubtypes")
                .SetValue(data, excludedSubtypes);

            var paramSubtype = "SubtypesData_None";
            var paramSubtypeReference = configuration.GetSection("param_subtype").ParseReference();
            if (paramSubtypeReference != null)
            {
                if (subtypeRegister.TryLookupId(
                    paramSubtypeReference.ToId(key, TemplateConstants.Subtype),
                    out var lookup,
                    out var _,
                    paramSubtypeReference.context))
                {
                    paramSubtype = lookup.Key;
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "paramSubtype")
                .SetValue(data, paramSubtype);

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
                .Field(typeof(CardEffectData), "additionalTooltips")
                .SetValue(data, tooltips.ToArray());

            var appliedToSelfVFXReference = configuration.GetSection("applied_to_self_vfx").ParseReference();
            var appliedToSelfVFXId = appliedToSelfVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "";
            if (vfxRegister.TryLookupId(appliedToSelfVFXId, out var appliedToSelfVFX, out var _, appliedToSelfVFXReference?.context))
            {
                AccessTools.Field(typeof(CardEffectData), "appliedToSelfVFX").SetValue(data, appliedToSelfVFX);
            }

            var appliedVFXReference = configuration.GetSection("applied_vfx").ParseReference();
            var appliedVFXId = appliedVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "";
            if (vfxRegister.TryLookupId(appliedVFXId, out var appliedVFX, out var _, appliedVFXReference?.context))
            {
                AccessTools.Field(typeof(CardEffectData), "appliedVFX").SetValue(data, appliedVFX);
            }
        }
    }
}
