using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using ShinyShoe;
using SimpleInjector;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TrainworksReloaded.Base.Card
{
    public class CardDataPipeline : IDataPipeline<IRegister<CardData>, CardData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<CardDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;
        private readonly IInstanceGenerator<CardData> generator;

        public CardDataPipeline(
            PluginAtlas atlas,
            IModLogger<CardDataPipeline> logger,
            IRegister<LocalizationTerm> termRegister,
            IGuidProvider guidProvider,
            IInstanceGenerator<CardData> generator
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
            this.generator = generator;
        }

        public List<IDefinition<CardData>> Run(IRegister<CardData> service)
        {
            // We load all cards and then finalize them to avoid dependency loops
            var processList = new List<IDefinition<CardData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadCards(service, config.Key, config.Value.Configuration));
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
        public List<CardDataDefinition> LoadCards(
            IRegister<CardData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CardDataDefinition>();
            foreach (var child in pluginConfig.GetSection("cards").GetChildren())
            {
                var data = LoadCardConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public CardDataDefinition? LoadCardConfiguration(
            IRegister<CardData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.Card, id);
            var namekey = $"CardData_nameKey-{name}";
            var descriptionKey = $"CardData_descriptionKey-{name}";
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();

            string guid;
            if (overrideMode.IsOverriding() && service.TryLookupName(id, out CardData? data, out var _))
            {
                logger.Log(LogLevel.Info, $"Overriding Card {id}... ");
                descriptionKey = data.GetOverrideDescriptionKey();
                namekey = data.GetNameKey();
                guid = data.GetID();
            }
            else
            {
                data = generator.CreateInstance();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
            }

            //handle id
            AccessTools.Field(typeof(CardData), "id").SetValue(data, guid);

            //handle names
            var localizationNameTerm = configuration.GetSection("names").ParseLocalizationTerm();
            if (localizationNameTerm != null)
            {
                AccessTools.Field(typeof(CardData), "nameKey").SetValue(data, namekey);
                localizationNameTerm.Key = namekey;
                termRegister.Register(namekey, localizationNameTerm);
            }

            //handle description
            var localizationDescTerm = configuration
                .GetSection("descriptions")
                .ParseLocalizationTerm();
            if (localizationDescTerm != null)
            {
                AccessTools
                    .Field(typeof(CardData), "overrideDescriptionKey")
                    .SetValue(data, descriptionKey);
                localizationDescTerm.Key = descriptionKey;
                termRegister.Register(descriptionKey, localizationDescTerm);
            }

            //handle tooltips
            var tooltips = (List<String>)
                    AccessTools.Field(typeof(CardData), "cardLoreTooltipKeys").GetValue(data);
            var loreTooltipsSection = configuration.GetSection("lore_tooltips");
            if (overrideMode == OverrideMode.Replace && loreTooltipsSection.Exists())
            {
                tooltips.Clear();
            }
            int tooltip_count = tooltips.Count;
            foreach (var tooltip in loreTooltipsSection.GetChildren())
            {
                var localizationTooltipTerm = tooltip.ParseLocalizationTerm();
                if (localizationTooltipTerm != null)
                {
                    string tooltipKey = $"CardData_tooltipKey{tooltip_count}-{name}";
                    if (localizationTooltipTerm.Key.IsNullOrEmpty())
                        localizationTooltipTerm.Key = tooltipKey;    
                    tooltips.Add(tooltipKey);
                    if (localizationTooltipTerm.HasTranslation())
                        termRegister.Register(tooltipKey, localizationTooltipTerm);
                    tooltip_count++;
                }
            }
            AccessTools.Field(typeof(CardData), "cardLoreTooltipKeys").SetValue(data, tooltips);

            //handle one-to-one values
            var defaultCost = (int)AccessTools.Field(typeof(CardData), "cost").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "cost")
                .SetValue(data, configuration.GetSection("cost").ParseInt() ?? defaultCost);

            var defaultCostType = overrideMode.IsNewContent() ? CardData.CostType.Default : (CardData.CostType)AccessTools.Field(typeof(CardData), "costType").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "costType")
                .SetValue(
                    data,
                    configuration.GetSection("cost_type").ParseCostType() ?? defaultCostType
                );

            var defaultCardType = overrideMode.IsNewContent() ? CardType.Spell : (CardType)AccessTools.Field(typeof(CardData), "cardType").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "cardType")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("type", "card_type").ParseCardType() ?? defaultCardType
                );

            var defaultInitCooldown = overrideMode.IsNewContent() ? 0 : (int)AccessTools.Field(typeof(CardData), "cooldownAtSpawn").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "cooldownAtSpawn")
                .SetValue(
                    data,
                    configuration.GetSection("initial_cooldown").ParseInt() ?? defaultInitCooldown
                );

            var defaultCooldown = overrideMode.IsNewContent() ? 0 : (int)AccessTools.Field(typeof(CardData), "cooldownAfterActivated").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "cooldownAfterActivated")
                .SetValue(data, configuration.GetSection("cooldown").ParseInt() ?? defaultCooldown);

            var defaultAbility = (bool)AccessTools.Field(typeof(CardData), "isUnitAbility").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "isUnitAbility")
                .SetValue(data, configuration.GetDeprecatedSection("ability", "is_an_ability").ParseBool() ?? defaultAbility);

            var defaultTargetsRoom = (bool)AccessTools.Field(typeof(CardData), "targetsRoom").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "targetsRoom")
                .SetValue(
                    data,
                    configuration.GetSection("targets_room").ParseBool() ?? defaultTargetsRoom
                );

            var defaultTargetless = (bool)AccessTools.Field(typeof(CardData), "targetless").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "targetless")
                .SetValue(
                    data,
                    configuration.GetSection("targetless").ParseBool() ?? defaultTargetless
                );

            var defaultRarity = overrideMode.IsNewContent() ? CollectableRarity.Common :
                 (CollectableRarity)AccessTools.Field(typeof(CardData), "rarity").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "rarity")
                .SetValue(data, configuration.GetSection("rarity").ParseRarity() ?? defaultRarity);

            var defaultDLC = overrideMode.IsNewContent() ? DLC.None :
                (DLC)AccessTools.Field(typeof(CardData), "requiredDLC").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "requiredDLC")
                .SetValue(data, configuration.GetDeprecatedSection("dlc", "required_dlc").ParseDLC() ?? defaultDLC);

            var defaultUnlockLevel = overrideMode.IsNewContent() ? 0 :
                (int)AccessTools.Field(typeof(CardData), "unlockLevel").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "unlockLevel")
                .SetValue(
                    data,
                    configuration.GetSection("unlock_level").ParseInt() ?? defaultUnlockLevel
                );

            var ignoreWhenCountingMastery = (bool)AccessTools.Field(typeof(CardData), "ignoreWhenCountingMastery").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "ignoreWhenCountingMastery")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("count_for_mastery", "ignore_when_counting_mastery").ParseBool()
                        ?? ignoreWhenCountingMastery
                );

            var hideInLogbook = (bool)AccessTools.Field(typeof(CardData), "hideInLogbook").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "hideInLogbook")
                .SetValue(
                    data,
                    configuration.GetSection("hide_in_logbook").ParseBool() ?? hideInLogbook
                );

            var initialKeyboardTarget = overrideMode.IsNewContent() ? CardInitialKeyboardTarget.FrontFriendly :
                 (CardInitialKeyboardTarget) AccessTools.Field(typeof(CardData), "initialKeyboardTarget").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "initialKeyboardTarget")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("target_assist", "initial_keyboard_target").ParseKeyboardTarget()
                        ?? initialKeyboardTarget
                );

            var canAbilityTargetOtherFloors = (bool)AccessTools.Field(typeof(CardData), "canAbilityTargetOtherFloors").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "canAbilityTargetOtherFloors")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("ability_effects_other_floors", "can_ability_target_other_floors").ParseBool()
                        ?? canAbilityTargetOtherFloors
                );

            var artistAttribution = overrideMode.IsNewContent() ? "" :
                (string)AccessTools.Field(typeof(CardData), "artistAttribution").GetValue(data);
            AccessTools
                .Field(typeof(CardData), "artistAttribution")
                .SetValue(
                    data,
                    configuration.GetSection("artist").ParseString() ?? artistAttribution
                );

            //register before filling in data using
            var modded = overrideMode.IsNewContent();
            if (modded)
                service.Register(name, data);

            return new CardDataDefinition(key, data, configuration, modded) { Id = id };
        }
    }
}
