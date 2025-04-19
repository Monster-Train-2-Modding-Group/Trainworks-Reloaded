using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using SimpleInjector;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeMaskPipeline : IDataPipeline<IRegister<CardUpgradeMaskData>, CardUpgradeMaskData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<CardUpgradeMaskPipeline> logger;

        public CardUpgradeMaskPipeline(
            PluginAtlas atlas,
            IModLogger<CardUpgradeMaskPipeline> logger
        )
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<CardUpgradeMaskData>> Run(IRegister<CardUpgradeMaskData> service)
        {
            // We load all cards and then finalize them to avoid dependency loops
            var processList = new List<IDefinition<CardUpgradeMaskData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadMasks(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        /// <summary>
        /// Loads the Card Definitions in
        /// </summary>
        /// <param name="service"></param>
        /// <param name="key"></param>
        /// <param name="pluginConfig"></param>
        /// <returns></returns>
        private List<CardUpgradeMaskDefinition> LoadMasks(
            IRegister<CardUpgradeMaskData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CardUpgradeMaskDefinition>();
            foreach (var child in pluginConfig.GetSection("upgrade_masks").GetChildren())
            {
                var data = LoadMaskConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CardUpgradeMaskDefinition? LoadMaskConfiguration(
            IRegister<CardUpgradeMaskData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.UpgradeMask, id);
            var data = ScriptableObject.CreateInstance<CardUpgradeMaskData>();
            data.name = name;

            //Process operators.
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredRaritiesOperator")
                .SetValue(data, configuration.GetSection("required_rarities_operator").ParseCompareOperator(defaultVal: "or"));
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedRaritiesOperator")
                .SetValue(data, configuration.GetSection("excluded_rarities_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredSubtypesOperator")
                .SetValue(data, configuration.GetSection("required_subtypes_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedSubtypesOperator")
                .SetValue(data, configuration.GetSection("excluded_subtypes_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredStatusEffectsOperator")
                .SetValue(data, configuration.GetSection("required_status_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedStatusEffectsOperator")
                .SetValue(data, configuration.GetSection("excluded_status_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredCardTraitsOperator")
                .SetValue(data, configuration.GetSection("required_traits_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedCardTraitsOperator")
                .SetValue(data, configuration.GetSection("excluded_traits_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredCardEffectsOperator")
                .SetValue(data, configuration.GetSection("required_effects_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedCardEffectsOperator")
                .SetValue(data, configuration.GetSection("excluded_effects_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredLinkedClansOperator")
                .SetValue(data, configuration.GetSection("required_class_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedLinkedClansOperator")
                .SetValue(data, configuration.GetSection("excluded_class_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredCardUpgradesOperator")
                .SetValue(data, configuration.GetSection("required_upgrade_operator").ParseCompareOperator());
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedCardUpgradesOperator")
                .SetValue(data, configuration.GetSection("excluded_upgrade_operator").ParseCompareOperator());

            //Process booleans
            AccessTools.Field(typeof(CardUpgradeMaskData), "requireXCost")
                .SetValue(data, configuration.GetSection("require_x_cost").ParseBool() ?? false);
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludeXCost")
                .SetValue(data, configuration.GetSection("exclude_x_cost").ParseBool() ?? false);
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludeNonAttackingMonsters")
                .SetValue(data, configuration.GetSection("exclude_non_attacking").ParseBool() ?? false);
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludeIfHasUnitAbility")
                .SetValue(data, configuration.GetSection("exclude_if_has_ability").ParseBool() ?? false);
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludeIfHasGraftedEquipment")
                .SetValue(data, configuration.GetSection("exclude_if_grafted").ParseBool() ?? false);
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludeIfHasAnyUpgrades")
                .SetValue(data, configuration.GetSection("exclude_if_upgraded").ParseBool() ?? false);
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludeIfHasNoUpgrades")
                .SetValue(data, configuration.GetSection("exclude_if_not_upgraded").ParseBool() ?? false);

            AccessTools.Field(typeof(CardUpgradeMaskData), "cardType")
                .SetValue(data, configuration.GetSection("card_type").ParseCardType() ?? CardType.Invalid);

            var cardTypes = configuration
                .GetSection("additional_card_types")
                .GetChildren()
                .Select(xs => xs.ParseCardType())
                .Where(xs => xs != null)
                .Cast<CardType>().ToList();
            AccessTools.Field(typeof(CardUpgradeMaskData), "additionalCardTypes").SetValue(data, cardTypes);

            CardTargetMode targetMode = CardTargetMode.All;
            foreach (var child in configuration.GetSection("card_target_mode").GetChildren())
            {
                var parsedMode = child.ParseCardTargetMode();
                if (parsedMode != null)
                {
                    targetMode |= parsedMode.Value;
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "cardTargetMode").SetValue(data, targetMode);

            var raritesRequired = configuration
                .GetSection("required_rarities")
                .GetChildren()
                .Select(xs => xs.ParseRarity())
                .Where(xs => xs != null)
                .Cast<CollectableRarity>()
                .ToList();
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredRarities").SetValue(data, raritesRequired);
            var raritesExcluded = configuration
                .GetSection("excluded_rarities")
                .GetChildren()
                .Select(xs => xs.ParseRarity())
                .Where(xs => xs != null)
                .Cast<CollectableRarity>()
                .ToList();
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedRarities").SetValue(data, raritesExcluded);

            var subtypesRequired = configuration
                .GetSection("required_subtypes")
                .GetChildren()
                .Select(xs => xs.ParseString())
                .Where(xs => xs != null)
                .Cast<string>()
                .ToList();
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredSubtypes").SetValue(data, subtypesRequired);
            var subtypesExcluded = configuration
                .GetSection("excluded_subtypes")
                .GetChildren()
                .Select(xs => xs.ParseString())
                .Where(xs => xs != null)
                .Cast<string>()
                .ToList();
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedSubtypes").SetValue(data, subtypesExcluded);

            var sizesRequired = configuration
                .GetSection("required_sizes")
                .GetChildren()
                .Select(xs => xs.ParseInt())
                .Where(xs => xs != null)
                .Cast<int>()
                .ToList();
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredSizes").SetValue(data, sizesRequired);
            var sizesExcluded = configuration
                .GetSection("excluded_sizes")
                .GetChildren()
                .Select(xs => xs.ParseInt())
                .Where(xs => xs != null)
                .Cast<int>()
                .ToList();
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedSizes").SetValue(data, sizesExcluded);

            var costRangeSection = configuration.GetSection("cost_range");
            if (costRangeSection != null)
            {
                int? x = costRangeSection.GetSection("x").ParseInt();
                int? y = costRangeSection.GetSection("y").ParseInt();
                if (x != null && y != null)
                {
                    var costRange = new Vector2(x.Value, y.Value);
                    AccessTools.Field(typeof(CardUpgradeMaskData), "costRange").SetValue(data, costRange);
                }
            }

            var disabledReason = configuration.GetSection("disabled_reason").ParseUpgradeDisabledReason() ?? CardState.UpgradeDisabledReason.NONE;
            AccessTools.Field(typeof(CardUpgradeMaskData), "upgradeDisabledReason").SetValue(data, disabledReason);

            List<string> requiredEffects = [];
            foreach (var child in configuration.GetSection("required_effects").GetChildren())
            {
                var effectType = ParseEffectType<CardEffectBase>(child, key, atlas);
                if (effectType != null)
                {
                    requiredEffects.Add(effectType);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredCardEffects").SetValue(data, requiredEffects);

            List<string> excludedEffects = [];
            foreach (var child in configuration.GetSection("excluded_effects").GetChildren())
            {
                var effectType = ParseEffectType<CardEffectBase>(child, key, atlas);
                if (effectType != null)
                {
                    excludedEffects.Add(effectType);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedCardEffects").SetValue(data, excludedEffects);

            List<string> requiredTraits = [];
            foreach (var child in configuration.GetSection("required_traits").GetChildren())
            {
                var effectType = ParseEffectType<CardTraitState>(child, key, atlas);
                if (effectType != null)
                {
                    requiredTraits.Add(effectType);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredCardTraits").SetValue(data, requiredTraits);

            List<string> excludedTraits = [];
            foreach (var child in configuration.GetSection("excluded_traits").GetChildren())
            {
                var effectType = ParseEffectType<CardTraitState>(child, key, atlas);
                if (effectType != null)
                {
                    excludedTraits.Add(effectType);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedCardTraits").SetValue(data, excludedTraits);

            service.Register(name, data);
            return new CardUpgradeMaskDefinition(key, data, configuration);
        }

        private string? ParseEffectType<T>(IConfiguration configuration, string key, PluginAtlas atlas)
        {
            var name = configuration.GetSection("name").ParseString();
            if (name == null)
            {
                return null;
            }
            var modReference = configuration.GetSection("mod_reference").Value ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !name.GetFullyQualifiedName<T>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                return null;
            }
            return fullyQualifiedName;
        }
    }
}
