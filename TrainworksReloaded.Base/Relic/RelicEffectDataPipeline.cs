using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Enums;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectDataPipeline : IDataPipeline<IRegister<RelicEffectData>, RelicEffectData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<RelicEffectDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;

        public RelicEffectDataPipeline(
            PluginAtlas atlas,
            IModLogger<RelicEffectDataPipeline> logger,
            IRegister<LocalizationTerm> termRegister
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
        }

        public List<IDefinition<RelicEffectData>> Run(IRegister<RelicEffectData> register)
        {
            var processList = new List<IDefinition<RelicEffectData>>();
            foreach (var config in atlas.PluginDefinitions)
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
                logger.Log(LogLevel.Error, $"Relic effect configuration missing required 'id' field");
                return null;
            }

            var name = key.GetId(TemplateConstants.RelicEffectData, effectId);
            var data = new RelicEffectData();

            // Handle effect class name
            var effectStateName = config.GetSection("name").Value;
            if (effectStateName == null)
                return null;

            var modReference = config.GetSection("mod_reference").Value ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !effectStateName.GetFullyQualifiedName<RelicEffectBase>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                logger.Log(LogLevel.Error, $"Failed to load relic effect state name {effectStateName} in {effectId} mod {modReference}, Make sure the class exists in {modReference} and that the class inherits from RelicEffectBase.");
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
                termRegister.Register(tooltipTitleKey, toolTipTitleTerm);
            }

            var tooltipBodyTerm = config.GetSection("tooltip_body").ParseLocalizationTerm();
            if (tooltipBodyTerm != null)
            {
                AccessTools.Field(typeof(RelicEffectData), "tooltipBodyKey").SetValue(data, tooltipBodyKey);
                tooltipBodyTerm.Key = tooltipBodyKey;
                termRegister.Register(tooltipBodyKey, tooltipBodyTerm);
            }

            // Handle source team
            var sourceTeam = config.GetSection("source_team").ParseTeamType() ?? Team.Type.None;
            AccessTools.Field(typeof(RelicEffectData), "paramSourceTeam").SetValue(data, sourceTeam);

            // Handle integer parameters
            var paramInt = config.GetSection("param_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramInt").SetValue(data, paramInt);

            var paramInt2 = config.GetSection("param_int_2").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramInt2").SetValue(data, paramInt2);

            var paramInt3 = config.GetSection("param_int_3").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramInt3").SetValue(data, paramInt3);


            // Handle float parameter
            var paramFloat = config.GetSection("param_float").ParseFloat() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramFloat").SetValue(data, paramFloat);

            // Handle integer range parameters
            var useIntRange = config.GetSection("use_int_range").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramUseIntRange").SetValue(data, useIntRange);

            var minInt = config.GetDeprecatedSection("min_int", "param_min_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramMinInt").SetValue(data, minInt);

            var maxInt = config.GetDeprecatedSection("max_int", "param_max_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(RelicEffectData), "paramMaxInt").SetValue(data, maxInt);

            // Handle string parameter
            var paramString = config.GetSection("param_string").ParseString() ?? "";
            AccessTools.Field(typeof(RelicEffectData), "paramString").SetValue(data, paramString);

            // Handle special character type
            var specialCharacterType = config.GetDeprecatedSection("special_character_type", "param_special_character_type").ParseSpecialCharacterType() ?? SpecialCharacterType.None;
            AccessTools.Field(typeof(RelicEffectData), "paramSpecialCharacterType").SetValue(data, specialCharacterType);

            // Handle boolean parameters
            var paramBool = config.GetSection("param_bool").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramBool").SetValue(data, paramBool);

            var paramBool2 = config.GetSection("param_bool_2").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramBool2").SetValue(data, paramBool2);

            var paramBool3 = config.GetSection("param_bool_3").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramBool3").SetValue(data, paramBool3);

            var paramBool4 = config.GetSection("param_bool_4").ParseBool() ?? false;
            AccessTools.Field(typeof(RelicEffectData), "paramBool4").SetValue(data, paramBool4);

            // Handle card type
            var cardType = config.GetDeprecatedSection("card_type", "param_card_type").ParseCardType() ?? CardType.Spell;
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
            var sourceCardTraitConfig = config.GetSection("source_card_trait");
            var sourceCardTraitName = ParseEffectType<CardTraitState>(sourceCardTraitConfig, key, atlas, effectId);
            AccessTools.Field(typeof(RelicEffectData), "sourceCardTraitParam").SetValue(data, sourceCardTraitName ?? "");

            // Handle target card trait
            var targetCardTraitConfig = config.GetSection("target_card_trait");
            var targetCardTraitName = ParseEffectType<CardTraitState>(targetCardTraitConfig, key, atlas, effectId);
            AccessTools.Field(typeof(RelicEffectData), "targetCardTraitParam").SetValue(data, targetCardTraitName ?? "");

            // Handle rarity ticket type
            var rarityTicketType = config.GetDeprecatedSection("rarity_ticket_type", "param_rarity_ticket_type").ParseRarityTicketType() ?? RarityTicketType.None;
            AccessTools.Field(typeof(RelicEffectData), "paramRarityTicketType").SetValue(data, rarityTicketType);

            // Handle card rarity type
            var cardRarityType = config.GetDeprecatedSection("card_rarity_type", "param_card_rarity_type").ParseRarity() ?? CollectableRarity.Common;
            AccessTools.Field(typeof(RelicEffectData), "paramCardRarityType").SetValue(data, cardRarityType);

            var rarityMultipliers = config.GetSection("param_rarity_multipliers").GetChildren().Select(x =>
                new RarityTicketMultiplier { rarityType = x.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common, ticketValueMultiplier = x.GetSection("multiplier").ParseFloat() ?? 1.0f }
            ).ToList();
            AccessTools.Field(typeof(RelicEffectData), "paramRarityMultipliers").SetValue(data, rarityMultipliers);

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

            service.Register(name, data);
            return new RelicEffectDataDefinition(key, data, config)
            {
                Id = effectId
            };
        }

        private string? ParseEffectType<T>(IConfigurationSection configuration, string key, PluginAtlas atlas, string id)
        {
            var reference = configuration.ParseReference();
            if (reference == null)
                return null;
            var name = reference.id;
            var modReference = reference.mod_reference ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (name.GetFullyQualifiedName<T>(assembly, out string? fullyQualifiedName))
            {
                return fullyQualifiedName;
            }
            logger.Log(LogLevel.Warning, $"Failed to load class name {name} in relic_effect {id} with mod reference {modReference}, Note that this isn't a reference to a CardTraitData, but a class that inherits from {typeof(T).Name}.");
            return null;
        }
    }
}