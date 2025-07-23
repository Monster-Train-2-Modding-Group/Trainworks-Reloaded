using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using SimpleInjector;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradePipeline : IDataPipeline<IRegister<CardUpgradeData>, CardUpgradeData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<CardUpgradePipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;

        public CardUpgradePipeline(
            PluginAtlas atlas,
            IModLogger<CardUpgradePipeline> logger,
            IRegister<LocalizationTerm> termRegister,
            IGuidProvider guidProvider
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
        }

        public List<IDefinition<CardUpgradeData>> Run(IRegister<CardUpgradeData> service)
        {
            // We load all upgrades and then finalize them to avoid dependency loops
            var processList = new List<IDefinition<CardUpgradeData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadUpgrades(service, config.Key, config.Value.Configuration));
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
        private List<CardUpgradeDefinition> LoadUpgrades(
            IRegister<CardUpgradeData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CardUpgradeDefinition>();
            foreach (var child in pluginConfig.GetSection("upgrades").GetChildren())
            {
                var data = LoadUpgradeConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CardUpgradeDefinition? LoadUpgradeConfiguration(
            IRegister<CardUpgradeData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.Upgrade, id);
            var titleKey = $"CardUpgrade_titleKey-{name}";
            var descriptionKey = $"CardUpgrade_descriptionKey-{name}";
            var notificationKey = $"CardUpgrade_notificationKey-{name}";
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();
            
            // TODO support cloning
            string guid;
            if (overrideMode.IsOverriding() && service.TryLookupName(id, out CardUpgradeData? data, out var _))
            {
                logger.Log(LogLevel.Info, $"Overriding Upgrade {id}...");
                titleKey = data.GetUpgradeTitleKey();
                descriptionKey = data.GetUpgradeDescriptionKey();
                notificationKey = data.GetUpgradeNotificationKey();
                guid = data.GetID();
            }
            else
            {
                data = ScriptableObject.CreateInstance<CardUpgradeData>();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
            }

            //handle id
            AccessTools.Field(typeof(CardUpgradeData), "id").SetValue(data, guid);

            //handle titles
            var localizationNameTerm = configuration.GetSection("titles").ParseLocalizationTerm();
            if (localizationNameTerm != null)
            {
                AccessTools
                    .Field(typeof(CardUpgradeData), "upgradeTitleKey")
                    .SetValue(data, titleKey);
                localizationNameTerm.Key = titleKey;
                termRegister.Register(titleKey, localizationNameTerm);
            }

            //handle description
            var localizationDescTerm = configuration
                .GetSection("descriptions")
                .ParseLocalizationTerm();
            if (localizationDescTerm != null)
            {
                AccessTools
                    .Field(typeof(CardUpgradeData), "upgradeDescriptionKey")
                    .SetValue(data, descriptionKey);
                localizationDescTerm.Key = descriptionKey;
                termRegister.Register(descriptionKey, localizationDescTerm);
            }

            //handle notifications
            var localizationNotificationTerm = configuration
                .GetSection("notifications")
                .ParseLocalizationTerm();
            if (localizationNotificationTerm != null)
            {
                AccessTools
                    .Field(typeof(CardUpgradeData), "upgradeNotificationKey")
                    .SetValue(data, notificationKey);
                localizationNotificationTerm.Key = notificationKey;
                termRegister.Register(notificationKey, localizationNotificationTerm);
            }

            //bools
            var allowSecondaryTooltipPlacement =
                    (bool)AccessTools
                        .Field(typeof(CardUpgradeData), "allowSecondaryTooltipPlacement")
                        .GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "allowSecondaryTooltipPlacement")
                .SetValue(
                    data,
                    configuration.GetSection("allow_secondary_tooltip_placement").ParseBool()
                        ?? allowSecondaryTooltipPlacement
                );

            var hideUpgradeIconOnCard =
                (bool)AccessTools
                        .Field(typeof(CardUpgradeData), "hideUpgradeIconOnCard")
                        .GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "hideUpgradeIconOnCard")
                .SetValue(
                    data,
                    configuration.GetSection("hide_icon_on_card").ParseBool()
                        ?? hideUpgradeIconOnCard
                );

            var useUpgradeHighlightTextTags =
                (bool)AccessTools
                        .Field(typeof(CardUpgradeData), "useUpgradeHighlightTextTags")
                        .GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "useUpgradeHighlightTextTags")
                .SetValue(
                    data,
                    configuration.GetSection("use_upgrade_highlight_tags").ParseBool()
                        ?? useUpgradeHighlightTextTags
                );

            var showUpgradeDescriptionOnCards =
                (bool)AccessTools
                        .Field(typeof(CardUpgradeData), "showUpgradeDescriptionOnCards")
                        .GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "showUpgradeDescriptionOnCards")
                .SetValue(
                    data,
                    configuration.GetSection("show_description_on_cards").ParseBool()
                        ?? showUpgradeDescriptionOnCards
                );

            var isUnique = (bool)AccessTools.Field(typeof(CardUpgradeData), "isUnique").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "isUnique")
                .SetValue(data, configuration.GetSection("is_unique").ParseBool() ?? isUnique);

            var restrictSizeToRoomCapacity =
                   (bool)AccessTools
                        .Field(typeof(CardUpgradeData), "restrictSizeToRoomCapacity")
                        .GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "restrictSizeToRoomCapacity")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("restrict_to_room_capacity", "restrict_size_to_room_capacity").ParseBool()
                        ?? restrictSizeToRoomCapacity
                );

            var doNotReplaceExistingUnitAbility =
                (bool)AccessTools
                        .Field(typeof(CardUpgradeData), "doNotReplaceExistingUnitAbility")
                        .GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "doNotReplaceExistingUnitAbility")
                .SetValue(
                    data,
                    configuration.GetSection("does_not_replace_ability").ParseBool()
                        ?? doNotReplaceExistingUnitAbility
                );

            //int
            var bonusDamage = (int)AccessTools.Field(typeof(CardUpgradeData), "bonusDamage").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "bonusDamage")
                .SetValue(data, configuration.GetSection("bonus_damage").ParseInt() ?? bonusDamage);

            var bonusHP = (int)AccessTools.Field(typeof(CardUpgradeData), "bonusHP").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "bonusHP")
                .SetValue(data, configuration.GetSection("bonus_hp").ParseInt() ?? bonusHP);

            var costReduction = (int)AccessTools.Field(typeof(CardUpgradeData), "costReduction").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "costReduction")
                .SetValue(
                    data,
                    configuration.GetSection("cost_reduction").ParseInt() ?? costReduction
                );

            var xCostReduction = (int)AccessTools.Field(typeof(CardUpgradeData), "xCostReduction").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "xCostReduction")
                .SetValue(
                    data,
                    configuration.GetSection("x_cost_reduction").ParseInt() ?? xCostReduction
                );

            var bonusHeal = (int)AccessTools.Field(typeof(CardUpgradeData), "bonusHeal").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "bonusHeal")
                .SetValue(data, configuration.GetSection("bonus_heal").ParseInt() ?? bonusHeal);

            var bonusSize = (int)AccessTools.Field(typeof(CardUpgradeData), "bonusSize").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "bonusSize")
                .SetValue(data, configuration.GetSection("bonus_size").ParseInt() ?? bonusSize);

            var bonusEquipment = (int)AccessTools.Field(typeof(CardUpgradeData), "bonusEquipment").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "bonusEquipment")
                .SetValue(
                    data,
                    configuration.GetSection("bonus_equipment").ParseInt() ?? bonusEquipment
                );

            var excludeFromClones = (bool)AccessTools.Field(typeof(CardUpgradeData), "excludeFromClones").GetValue(data);
            AccessTools
                .Field(typeof(CardUpgradeData), "excludeFromClones")
                .SetValue(
                    data,
                    configuration.GetSection("exclude_from_clones").ParseBool() ?? excludeFromClones
                );

            //List<String>
            var removeTraitUpgrades = data.GetRemoveTraitUpgrades() ?? [];
            var removeTraitUpgradeConfig = configuration.GetSection("remove_trait_upgrades");
            if (overrideMode == OverrideMode.Replace && removeTraitUpgradeConfig.Exists())
            {
                removeTraitUpgrades.Clear();
            }    
            var upgradeReferences = removeTraitUpgradeConfig 
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in upgradeReferences)
            {
                var traitStateName = reference.id;
                var modReference = reference.mod_reference ?? key;
                var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
                if (
                    !traitStateName.GetFullyQualifiedName<CardTraitState>(
                        assembly,
                        out string? fullyQualifiedName
                    )
                )
                {
                    logger.Log(LogLevel.Warning, $"Failed to load trait state name {traitStateName} in upgrade {id} with mod reference {modReference}, Note that this isn't a reference to a CardTraitData, but a class that inherits from CardTraitState.");
                    continue;
                }
                removeTraitUpgrades.Add(fullyQualifiedName);
            }
            AccessTools
                .Field(typeof(CardUpgradeData), "removeTraitUpgrades")
                .SetValue(data, removeTraitUpgrades);

            var modded = overrideMode.IsNewContent();
            if (modded)
                service.Register(name, data);
            return new CardUpgradeDefinition(key, data, configuration, modded);
        }
    }
}
