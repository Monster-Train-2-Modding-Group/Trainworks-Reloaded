using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Effect;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.StatusEffects
{
    public class StatusEffectDataPipeline : IDataPipeline<IRegister<StatusEffectData>, StatusEffectData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<StatusEffectDataPipeline> logger;
        private readonly IRegister<ReplacementStringData> replacementTextRegister;
        private readonly IRegister<LocalizationTerm> termRegister;

        public StatusEffectDataPipeline(
            PluginAtlas atlas,
            IModLogger<StatusEffectDataPipeline> logger,
            IRegister<ReplacementStringData> replacementTextRegister,
            IRegister<LocalizationTerm> termRegister
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.replacementTextRegister = replacementTextRegister;
            this.termRegister = termRegister;
        }

        public List<IDefinition<StatusEffectData>> Run(IRegister<StatusEffectData> service)
        {
            var processList = new List<IDefinition<StatusEffectData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadStatusEffects(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private IEnumerable<IDefinition<StatusEffectData>> LoadStatusEffects(IRegister<StatusEffectData> service, string key, IConfiguration configuration)
        {
            var processList = new List<StatusEffectDataDefinition>();
            foreach (var child in configuration.GetSection("status_effects").GetChildren())
            {
                var data = LoadEffectConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private StatusEffectDataDefinition? LoadEffectConfiguration(IRegister<StatusEffectData> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var internalID = key.GetId(TemplateConstants.StatusEffect, id);
            var statusId = internalID.ToLowerInvariant();

            var data = new StatusEffectData();
            AccessTools.Field(typeof(StatusEffectData), "statusId").SetValue(data, statusId);

            var statusEffectStateName = configuration.GetSection("class_name").Value;
            if (statusEffectStateName == null)
                return null;

            // Because the statusId is coupled with the class, only search the Assembly defining the status effect.
            var assembly = atlas.PluginDefinitions[key].Assembly;
            if (!statusEffectStateName.GetFullyQualifiedName<StatusEffectState>(
                assembly,
                out string? fullyQualifiedName)
            )
            {
                return null;
            }
            AccessTools.Field(typeof(StatusEffectData), "statusEffectStateName").SetValue(data, fullyQualifiedName);

            // The localization keys per status are generated based a based on a base key name.
            var baseKey = "StatusEffect_" + statusId;

            var localizationNameTerm = configuration.GetSection("names").ParseLocalizationTerm();
            if (localizationNameTerm != null)
            {
                localizationNameTerm.Key = baseKey + "_CardText";
                termRegister.Register(localizationNameTerm.Key, localizationNameTerm);
            }

            var localizationStackNameTerm = configuration.GetSection("stackable_names").ParseLocalizationTerm();
            if (localizationStackNameTerm != null)
            {
                localizationStackNameTerm.Key = baseKey + "_Stack_CardText";
                termRegister.Register(localizationStackNameTerm.Key, localizationStackNameTerm);
            }

            var localizationCardTooltipTerm = configuration.GetSection("card_tooltips").ParseLocalizationTerm();
            if (localizationCardTooltipTerm != null)
            {
                localizationCardTooltipTerm.Key = baseKey + "_CardTooltipText";
                termRegister.Register(localizationCardTooltipTerm.Key, localizationCardTooltipTerm);
            }

            var localizationCharacterTooltipTerm = configuration.GetSection("character_tooltips").ParseLocalizationTerm();
            if (localizationCharacterTooltipTerm != null)
            {
                localizationCharacterTooltipTerm.Key = baseKey + "_CharacterTooltipText";
                termRegister.Register(localizationCharacterTooltipTerm.Key, localizationCharacterTooltipTerm);
            }

            var localizationNotificationTerm = configuration.GetSection("notifications").ParseLocalizationTerm();
            if (localizationNotificationTerm != null)
            {
                localizationNotificationTerm.Key = baseKey + "_NotificationText";
                termRegister.Register(localizationNotificationTerm.Key, localizationNotificationTerm);
            }

            var localizationReplacementTerm = configuration.GetSection("replacement_texts").ParseLocalizationTerm();
            if (localizationReplacementTerm != null)
            {
                var replacement_key = $"{key}_{id}";
                var loc_key = $"ReplacementStringsData_replacement-{replacement_key}";
                localizationReplacementTerm.Key = loc_key;
                termRegister.Register(localizationReplacementTerm.Key, localizationReplacementTerm);

                ReplacementStringData replacement = new();
                AccessTools.Field(typeof(ReplacementStringData), "_keyword").SetValue(replacement, replacement_key);
                AccessTools.Field(typeof(ReplacementStringData), "_replacement").SetValue(replacement, localizationReplacementTerm.Key);
                replacementTextRegister.Register(replacement_key, replacement);
            }
            
            var appliedSFXName = "";
            AccessTools
                .Field(typeof(StatusEffectData), "appliedSFXName")
                .SetValue(data, configuration.GetSection("applied_sfx").ParseString() ?? appliedSFXName);

            var triggeredSFXName = "";
            AccessTools
                .Field(typeof(StatusEffectData), "triggeredSFXName")
                .SetValue(data, configuration.GetSection("triggered_sfx").ParseString() ?? triggeredSFXName);

            var displayCategory = StatusEffectData.DisplayCategory.Positive;
            AccessTools
                .Field(typeof(StatusEffectData), "displayCategory")
                .SetValue(data, configuration.GetSection("display_category").ParseDisplayCategory() ?? displayCategory);

            var vfxDisplayType = StatusEffectData.VFXDisplayType.Default;
            AccessTools
                .Field(typeof(StatusEffectData), "vfxDisplayType")
                .SetValue(data, configuration.GetSection("vfx_display_type").ParseVFXDisplayType() ?? vfxDisplayType);

            var triggerStage = StatusEffectData.TriggerStage.None;
            AccessTools
                .Field(typeof(StatusEffectData), "triggerStage")
                .SetValue(data, configuration.GetSection("trigger_stage").ParseTriggerStage() ?? triggerStage);

            var additionalTriggerStages = configuration
                .GetSection("additional_trigger_stages")
                .GetChildren()
                .Select(xs => xs.ParseTriggerStage() ?? StatusEffectData.TriggerStage.None)
                .ToList();
            AccessTools
                .Field(typeof(StatusEffectData), "additionalTriggerStages")
                .SetValue(data, additionalTriggerStages);

            var hidden = false;
            AccessTools
                .Field(typeof(StatusEffectData), "hidden")
                .SetValue(data, configuration.GetSection("hidden").ParseBool() ?? hidden);

            var removeStackAtEndOfTurn = false;
            AccessTools
                .Field(typeof(StatusEffectData), "removeStackAtEndOfTurn")
                .SetValue(data, configuration.GetSection("remove_stack_at_end_of_turn").ParseBool() ?? removeStackAtEndOfTurn);

            var removeAtEndOfTurn = false;
            AccessTools
                .Field(typeof(StatusEffectData), "removeAtEndOfTurn")
                .SetValue(data, configuration.GetSection("remove_at_end_of_turn").ParseBool() ?? removeAtEndOfTurn);

            var removeWhenTriggered = false;
            AccessTools
                .Field(typeof(StatusEffectData), "removeWhenTriggered")
                .SetValue(data, configuration.GetSection("remove_when_triggered").ParseBool() ?? removeWhenTriggered);

            var removeWhenTriggeredAfterCardPlayed = false;
            AccessTools
                .Field(typeof(StatusEffectData), "removeWhenTriggeredAfterCardPlayed")
                .SetValue(data, configuration.GetSection("remove_when_triggered_after_card_played").ParseBool() ?? removeWhenTriggeredAfterCardPlayed);

            var removeAtEndOfTurnIfTriggered = false;
            AccessTools
                .Field(typeof(StatusEffectData), "removeAtEndOfTurnIfTriggered")
                .SetValue(data, configuration.GetSection("remove_at_end_of_turn_if_triggered").ParseBool() ?? removeAtEndOfTurnIfTriggered);

            var isStackable = true;
            AccessTools
                .Field(typeof(StatusEffectData), "isStackable")
                .SetValue(data, configuration.GetSection("is_stackable").ParseBool() ?? isStackable);

            var isPropagatable = false;
            AccessTools
                .Field(typeof(StatusEffectData), "isPropagatable")
                .SetValue(data, configuration.GetSection("is_propagatable").ParseBool() ?? isPropagatable);

            var scalesInEndless = true;
            AccessTools
                .Field(typeof(StatusEffectData), "scalesInEndless")
                .SetValue(data, configuration.GetSection("scales_in_endless").ParseBool() ?? scalesInEndless);

            var excludeHeroPropagation = false;
            AccessTools
                .Field(typeof(StatusEffectData), "excludeHeroPropagation")
                .SetValue(data, configuration.GetSection("exclude_hero_propagation").ParseBool() ?? excludeHeroPropagation);

            var excludeMonsterPropagation = false;
            AccessTools
                .Field(typeof(StatusEffectData), "excludeMonsterPropagation")
                .SetValue(data, configuration.GetSection("exclude_monster_propagation").ParseBool() ?? excludeMonsterPropagation);

            var showStackCount = true;
            AccessTools
                .Field(typeof(StatusEffectData), "showStackCount")
                .SetValue(data, configuration.GetSection("show_stack_count").ParseBool() ?? showStackCount);

            var showNotificationsOnRemoval = true;
            AccessTools
                .Field(typeof(StatusEffectData), "showNotificationsOnRemoval")
                .SetValue(data, configuration.GetSection("show_notifications_on_removal").ParseBool() ?? showNotificationsOnRemoval);

            var showOnPyreHeart = false;
            AccessTools
                .Field(typeof(StatusEffectData), "showOnPyreHeart")
                .SetValue(data, configuration.GetSection("show_on_pyre_heart").ParseBool() ?? showOnPyreHeart);

            var paramStr = "";
            AccessTools
                .Field(typeof(StatusEffectData), "paramStr")
                .SetValue(data, configuration.GetSection("param_str").ParseString() ?? paramStr);

            var paramInt = 0;
            AccessTools
                .Field(typeof(StatusEffectData), "paramInt")
                .SetValue(data, configuration.GetSection("param_int").ParseInt() ?? paramInt);

            var paramSecondaryInt = 0;
            AccessTools
                .Field(typeof(StatusEffectData), "paramSecondaryInt")
                .SetValue(data, configuration.GetSection("param_secondary_int").ParseInt() ?? paramSecondaryInt);

            var paramFloat = 0f;
            AccessTools
                .Field(typeof(StatusEffectData), "paramFloat")
                .SetValue(data, configuration.GetSection("param_float").ParseFloat() ?? paramFloat);

            var allowSecondaryTooltipPlacement = false;
            AccessTools
                .Field(typeof(StatusEffectData), "allowSecondaryTooltipPlacement")
                .SetValue(data, configuration.GetSection("allow_secondary_tooltip_placement").ParseBool() ?? allowSecondaryTooltipPlacement);


            var allowSecondaryUIPlacement = false;
            AccessTools
                .Field(typeof(StatusEffectData), "allowSecondaryUIPlacement")
                .SetValue(data, configuration.GetSection("allow_secondary_ui_placement").ParseBool() ?? allowSecondaryUIPlacement);

            service.Register(internalID, data);
            return new StatusEffectDataDefinition(key, data, configuration)
            {
                Id = statusId,
            };
        }
    }
}
