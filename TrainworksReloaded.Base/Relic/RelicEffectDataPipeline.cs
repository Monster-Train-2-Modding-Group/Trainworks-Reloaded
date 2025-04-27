using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectDataPipeline : IDataPipeline<IRegister<RelicEffectData>, RelicEffectData>
    {
        private readonly PluginAtlas _atlas;
        private readonly IModLogger<RelicEffectDataPipeline> _logger;
        private readonly IRegister<LocalizationTerm> _termRegister;

        public RelicEffectDataPipeline(
            PluginAtlas atlas,
            IModLogger<RelicEffectDataPipeline> logger,
            IRegister<LocalizationTerm> termRegister
        )
        {
            _atlas = atlas;
            _logger = logger;
            _termRegister = termRegister;
        }

        public List<IDefinition<RelicEffectData>> Run(IRegister<RelicEffectData> register)
        {
            var processList = new List<IDefinition<RelicEffectData>>();
            foreach (var config in _atlas.PluginDefinitions)
            {
                processList.AddRange(LoadRelicEffects(register, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<RelicEffectDataDefinition> LoadRelicEffects(
            IRegister<RelicEffectData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<RelicEffectDataDefinition>();
            foreach (var child in pluginConfig.GetSection("relic_effects").GetChildren())
            {
                var data = LoadRelicEffectConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private RelicEffectDataDefinition? LoadRelicEffectConfiguration(
            IRegister<RelicEffectData> service,
            string key,
            IConfigurationSection config
        )
        {
            var effectId = config.GetSection("id").ParseString();
            if (string.IsNullOrEmpty(effectId))
            {
                _logger.Log(LogLevel.Error, $"Relic effect configuration missing required 'id' field");
                return null;
            }

            var name = key.GetId(TemplateConstants.RelicEffectData, effectId);
            var data = new RelicEffectData();

            // Handle effect class name
            var effectStateName = config.GetSection("name").Value;
            if (effectStateName == null)
                return null;

            var modReference = config.GetSection("mod_reference").Value ?? key;
            var assembly = _atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !effectStateName.GetFullyQualifiedName<RelicEffectBase>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                return null;
            }
            AccessTools.Field(typeof(RelicEffectData), "relicEffectClassName").SetValue(data, fullyQualifiedName);


            // Handle tooltip keys
            var tooltipTitleKey = $"RelicEffectData_tooltipTitleKey-{name}";
            var tooltipBodyKey = $"RelicEffectData_tooltipBodyKey-{name}";

            // Handle name localization
            var toolTipTitleTerm = config.GetSection("tooltip_titles").ParseLocalizationTerm();
            if (toolTipTitleTerm != null)
            {
                AccessTools.Field(typeof(RelicEffectData), "tooltipTitleKey").SetValue(data, tooltipTitleKey);
                toolTipTitleTerm.Key = tooltipTitleKey;
                _termRegister.Register(tooltipTitleKey, toolTipTitleTerm);
            }

            var tooltipBodyTerm = config.GetSection("tooltip_body").ParseLocalizationTerm();
            if (tooltipBodyTerm != null)
            {
                AccessTools.Field(typeof(RelicEffectData), "tooltipBodyKey").SetValue(data, tooltipBodyKey);
                tooltipBodyTerm.Key = tooltipBodyKey;
                _termRegister.Register(tooltipBodyKey, tooltipBodyTerm);
            }

            // Handle source team
            var sourceTeam = config.GetSection("source_team").ParseTeamType() ?? Team.Type.None;
            AccessTools.Field(typeof(RelicEffectData), "paramSourceTeam").SetValue(data, sourceTeam);

            // Handle integer parameters
            var paramInt = config.GetSection("param_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramInt").SetValue(data, paramInt);

            var paramInt2 = config.GetSection("param_int_2").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramInt2").SetValue(data, paramInt2);
            
            // Handle float parameter
            var paramFloat = config.GetSection("param_float").ParseFloat() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramFloat").SetValue(data, paramFloat);

            // Handle integer range parameters
            var useIntRange = config.GetSection("use_int_range").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramUseIntRange").SetValue(data, useIntRange);

            var minInt = config.GetSection("min_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramMinInt").SetValue(data, minInt);

            var maxInt = config.GetSection("max_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramMaxInt").SetValue(data, maxInt);

            // Handle string parameter
            var paramString = config.GetSection("param_string").ParseString() ?? "";
            AccessTools.Field(typeof(RelicEffectData), "paramString").SetValue(data, paramString);

            var paramTrigger = config.GetSection("param_trigger").ParseString() ?? "";
            AccessTools.Field(typeof(RelicEffectData), "paramTrigger").SetValue(data, paramTrigger);

            // Handle character subtype
            var characterSubtype = config.GetSection("character_subtype").ParseString() ?? "SubtypesData_None";
            AccessTools.Field(typeof(RelicEffectData), "paramCharacterSubtype").SetValue(data, characterSubtype);


            // Handle excluded character subtypes
            var excludedSubtypes = config.GetSection("excluded_character_subtypes").GetChildren()
                .Select(x => x.Value)
                .Where(x => x != null)
                .ToArray() ?? [];
            AccessTools.Field(typeof(RelicEffectData), "paramExcludeCharacterSubtypes").SetValue(data, excludedSubtypes);


            // Handle special character type
            var specialCharacterType = config.GetSection("special_character_type").ParseSpecialCharacterType() ?? SpecialCharacterType.None;
            AccessTools.Field(typeof(RelicEffectData), "paramSpecialCharacterType").SetValue(data, specialCharacterType);

            // Handle boolean parameters
            var paramBool = config.GetSection("param_bool").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramBool").SetValue(data, paramBool);

            var paramBool2 = config.GetSection("param_bool_2").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramBool2").SetValue(data, paramBool2);

            // Handle target mode
            var targetMode = config.GetSection("target_mode").ParseTargetMode() ?? TargetMode.Room;
            AccessTools.Field(typeof(RelicEffectData), "paramTargetMode").SetValue(data, targetMode);

            // Handle card type
            var cardType = config.GetSection("card_type").ParseCardType() ?? CardType.Spell;
            AccessTools.Field(typeof(RelicEffectData), "paramCardType").SetValue(data, cardType);


            // Handle notification suppression
            var notificationSuppressed = config.GetSection("notification_suppressed").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "notificationSuppressed").SetValue(data, notificationSuppressed);

            // Handle trigger tooltips suppression
            var triggerTooltipsSuppressed = config.GetSection("trigger_tooltips_suppressed").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "triggerTooltipsSuppressed").SetValue(data, triggerTooltipsSuppressed);

            // Handle relic scaling count
            var relicScalingCount = config.GetSection("relic_scaling_count").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "relicScalingCount").SetValue(data, relicScalingCount);


            // Handle source card trait
            var sourceCardTrait = config.GetSection("source_card_trait").ParseString();
            if (sourceCardTrait != null && sourceCardTrait.GetFullyQualifiedName<CardTraitState>(
                    assembly,
                    out string? sourceCardTraitName
                ))
            {
                AccessTools.Field(typeof(RelicEffectData), "sourceCardTraitParam").SetValue(data, sourceCardTraitName);
            }
            else
            {
                AccessTools.Field(typeof(RelicEffectData), "sourceCardTraitParam").SetValue(data, "");
            }

            // Handle target card trait
            var targetCardTrait = config.GetSection("target_card_trait").ParseString();
            if (targetCardTrait != null && targetCardTrait.GetFullyQualifiedName<CardTraitState>(
                    assembly,
                    out string? targetCardTraitName
                ))
            {
                AccessTools.Field(typeof(RelicEffectData), "targetCardTraitParam").SetValue(data, targetCardTraitName);
            }
            else
            {
                AccessTools.Field(typeof(RelicEffectData), "targetCardTraitParam").SetValue(data, "");
            }

            // Handle rarity ticket type
            var rarityTicketType = config.GetSection("rarity_ticket_type").ParseRarityTicketType() ?? RarityTicketType.None;
            AccessTools.Field(typeof(RelicEffectData), "paramRarityTicketType").SetValue(data, rarityTicketType);

            // Handle card rarity type
            var cardRarityType = config.GetSection("card_rarity_type").ParseRarity() ?? CollectableRarity.Common;
            AccessTools.Field(typeof(RelicEffectData), "paramCardRarityType").SetValue(data, cardRarityType);

        

            //Handle cardTriggers
            var cardTriggers = config.GetSection("card_triggers").GetChildren()
                .Select(x => x.ParseCardTriggerType())
                .Where(x => x != null)
                .Select(x => (CardTriggerType)x!)
                .ToList();
            if (cardTriggers.Count != 0)
            {
                AccessTools.Field(typeof(RelicEffectData), "cardTriggers").SetValue(data, cardTriggers);
            }

            //handle relic effect conditions
            var relicEffectConditions = new List<RelicEffectCondition>();
            var relicEffectConditionsConfig = config.GetSection("relic_effect_conditions").GetChildren();
            foreach (var relicEffectConditionConfig in relicEffectConditionsConfig)
            {
                // TODO: implement relic effect conditions
            }
            AccessTools.Field(typeof(RelicEffectData), "effectConditions").SetValue(data, relicEffectConditions);

            // Handle VFX
            // var vfxId = config.GetSection("applied_vfx").ParseString() ?? "";
            // AccessTools.Field(typeof(RelicEffectData), "appliedVfx").SetValue(data, vfxId);

            service.Register(name, data);
            return new RelicEffectDataDefinition(key, data, config){
                Id = effectId
            };
        }
    }
}
