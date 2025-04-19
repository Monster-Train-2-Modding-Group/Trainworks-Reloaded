using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using SimpleInjector;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Effect
{
    public class CardEffectDataPipeline : IDataPipeline<IRegister<CardEffectData>, CardEffectData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<CardEffectDataPipeline> logger;

        public CardEffectDataPipeline(PluginAtlas atlas, IModLogger<CardEffectDataPipeline> logger)
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<CardEffectData>> Run(IRegister<CardEffectData> service)
        {
            var processList = new List<IDefinition<CardEffectData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadEffects(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        public List<CardEffectDefinition> LoadEffects(
            IRegister<CardEffectData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CardEffectDefinition>();
            foreach (var child in pluginConfig.GetSection("effects").GetChildren())
            {
                var data = LoadEffectConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public CardEffectDefinition? LoadEffectConfiguration(
            IRegister<CardEffectData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId("Effect", id);
            var data = new CardEffectData();

            // EffectClass
            var effectStateName = configuration.GetSection("name").Value;
            if (effectStateName == null)
                return null;

            var modReference = configuration.GetSection("mod_reference").Value ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !effectStateName.GetFullyQualifiedName<CardEffectBase>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                logger.Log(LogLevel.Error, $"Failed to load effect state name {effectStateName} in {name} with mod reference {modReference}");
                return null;
            }
            AccessTools
                .Field(typeof(CardEffectData), "effectStateName")
                .SetValue(data, fullyQualifiedName);

            //strings
            var targetCharacterSubtype = "SubtypesData_None";
            AccessTools
                .Field(typeof(CardEffectData), "targetCharacterSubtype")
                .SetValue(
                    data,
                    configuration.GetSection("target_subtype").ParseString()
                        ?? targetCharacterSubtype
                );

            var paramStr = "";
            AccessTools
                .Field(typeof(CardEffectData), "paramStr")
                .SetValue(data, configuration.GetSection("param_str").ParseString() ?? paramStr);

            var paramSubtype = "SubtypesData_None";
            AccessTools
                .Field(typeof(CardEffectData), "paramSubtype")
                .SetValue(
                    data,
                    configuration.GetSection("param_subtype").ParseString() ?? paramSubtype
                );

            //bools
            var suppressPyreRoomFocus = false;
            AccessTools
                .Field(typeof(CardEffectData), "suppressPyreRoomFocus")
                .SetValue(
                    data,
                    configuration.GetSection("supress_pyre_room_focus").ParseBool()
                        ?? suppressPyreRoomFocus
                );

            var targetIgnoreBosses = false;
            AccessTools
                .Field(typeof(CardEffectData), "targetIgnoreBosses")
                .SetValue(
                    data,
                    configuration.GetSection("target_ignore_bosses").ParseBool()
                        ?? targetIgnoreBosses
                );

            var filterBasedOnMainSubClass = false;
            AccessTools
                .Field(typeof(CardEffectData), "filterBasedOnMainSubClass")
                .SetValue(
                    data,
                    configuration.GetSection("filter_on_main_subclass").ParseBool()
                        ?? filterBasedOnMainSubClass
                );

            var copyModifiersFromSource = false;
            AccessTools
                .Field(typeof(CardEffectData), "copyModifiersFromSource")
                .SetValue(
                    data,
                    configuration.GetSection("copy_modifiers").ParseBool()
                        ?? copyModifiersFromSource
                );

            var ignoreTemporaryModifiersFromSource = false;
            AccessTools
                .Field(typeof(CardEffectData), "ignoreTemporaryModifiersFromSource")
                .SetValue(
                    data,
                    configuration.GetSection("ignore_temporary_modifiers").ParseBool()
                        ?? ignoreTemporaryModifiersFromSource
                );

            var showPyreNotification = false;
            AccessTools
                .Field(typeof(CardEffectData), "showPyreNotification")
                .SetValue(
                    data,
                    configuration.GetSection("show_pyre_notification").ParseBool()
                        ?? showPyreNotification
                );

            var shouldTest = true;
            AccessTools
                .Field(typeof(CardEffectData), "shouldTest")
                .SetValue(data, configuration.GetSection("should_test").ParseBool() ?? shouldTest);

            var shouldCancelSubsequentEffectsIfTestFails = false;
            AccessTools
                .Field(typeof(CardEffectData), "shouldCancelSubsequentEffectsIfTestFails")
                .SetValue(
                    data,
                    configuration.GetSection("cancel_subsequent_effects_on_failure").ParseBool()
                        ?? shouldCancelSubsequentEffectsIfTestFails
                );

            var shouldFailToCastIfTestFails = false;
            AccessTools
                .Field(typeof(CardEffectData), "shouldFailToCastIfTestFails")
                .SetValue(
                    data,
                    configuration.GetSection("fail_to_cast_on_failure").ParseBool()
                        ?? shouldFailToCastIfTestFails
                );

            var shouldSkipSubsequentEffectsPreviews = false;
            AccessTools
                .Field(typeof(CardEffectData), "shouldSkipSubsequentEffectsPreviews")
                .SetValue(
                    data,
                    configuration.GetSection("skip_subsequent_previews").ParseBool()
                        ?? shouldSkipSubsequentEffectsPreviews
                );

            var useIntRange = false;
            AccessTools
                .Field(typeof(CardEffectData), "useIntRange")
                .SetValue(
                    data,
                    configuration.GetSection("use_int_range").ParseBool() ?? useIntRange
                );

            var paramBool = false;
            AccessTools
                .Field(typeof(CardEffectData), "paramBool")
                .SetValue(data, configuration.GetSection("param_bool").ParseBool() ?? paramBool);

            var paramBool2 = false;
            AccessTools
                .Field(typeof(CardEffectData), "paramBool2")
                .SetValue(data, configuration.GetSection("param_bool_2").ParseBool() ?? paramBool2);

            var useStatusEffectStackMultiplier = false;
            AccessTools
                .Field(typeof(CardEffectData), "useStatusEffectStackMultiplier")
                .SetValue(
                    data,
                    configuration.GetSection("use_status_effect_multiplier").ParseBool()
                        ?? useStatusEffectStackMultiplier
                );

            var useHealthMissingStackMultiplier = false;
            AccessTools
                .Field(typeof(CardEffectData), "useHealthMissingStackMultiplier")
                .SetValue(
                    data,
                    configuration.GetSection("use_health_missing_multiplier").ParseBool()
                        ?? useHealthMissingStackMultiplier
                );

            var useMagicPowerMultiplier = false;
            AccessTools
                .Field(typeof(CardEffectData), "useMagicPowerMultiplier")
                .SetValue(
                    data,
                    configuration.GetSection("use_magic_power_multiplier").ParseBool()
                        ?? useMagicPowerMultiplier
                );

            //ints
            var paramInt = 0;
            AccessTools
                .Field(typeof(CardEffectData), "paramInt")
                .SetValue(data, configuration.GetSection("param_int").ParseInt() ?? paramInt);

            var additionalParamInt = 0;
            AccessTools
                .Field(typeof(CardEffectData), "additionalParamInt")
                .SetValue(
                    data,
                    configuration.GetSection("param_int_2").ParseInt() ?? additionalParamInt
                );

            var additionalParamInt1 = 0;
            AccessTools
                .Field(typeof(CardEffectData), "additionalParamInt1")
                .SetValue(
                    data,
                    configuration.GetSection("param_int_3").ParseInt() ?? additionalParamInt1
                );

            var paramMinInt = 0;
            AccessTools
                .Field(typeof(CardEffectData), "paramMinInt")
                .SetValue(
                    data,
                    configuration.GetSection("param_min_int").ParseInt() ?? paramMinInt
                );

            var paramMaxInt = 0;
            AccessTools
                .Field(typeof(CardEffectData), "paramMaxInt")
                .SetValue(
                    data,
                    configuration.GetSection("param_max_int").ParseInt() ?? paramMaxInt
                );

            //floats
            var paramMultiplier = 1.0f;
            AccessTools
                .Field(typeof(CardEffectData), "paramMultiplier")
                .SetValue(
                    data,
                    configuration.GetSection("param_multiplier").ParseFloat() ?? paramMultiplier
                );

            //target mode
            var targetMode = TargetMode.Room;
            AccessTools
                .Field(typeof(CardEffectData), "targetMode")
                .SetValue(
                    data,
                    configuration.GetSection("target_mode").ParseTargetMode() ?? targetMode
                );

            //health filter
            var targetModeHealthFilter = CardEffectData.HealthFilter.Both;
            AccessTools
                .Field(typeof(CardEffectData), "targetModeHealthFilter")
                .SetValue(
                    data,
                    configuration.GetSection("target_mode_health_filter").ParseHealthFilter()
                        ?? targetModeHealthFilter
                );

            //target team
            var targetTeamType = Team.Type.None;
            AccessTools
                .Field(typeof(CardEffectData), "targetTeamType")
                .SetValue(
                    data,
                    configuration.GetSection("target_team").ParseTeamType() ?? targetTeamType
                );

            //target card
            var targetCardType = CardType.Spell;
            AccessTools
                .Field(typeof(CardEffectData), "targetCardType")
                .SetValue(
                    data,
                    configuration.GetSection("target_card_type").ParseCardType() ?? targetCardType
                );

            //selection mode
            var targetCardSelectionMode = CardEffectData.CardSelectionMode.ChooseToHand;
            AccessTools
                .Field(typeof(CardEffectData), "targetCardSelectionMode")
                .SetValue(
                    data,
                    configuration.GetSection("target_selection_mode").ParseCardSelectionMode()
                        ?? targetCardSelectionMode
                );

            //selection mode
            var animToPlay = CharacterUI.Anim.None;
            AccessTools
                .Field(typeof(CardEffectData), "animToPlay")
                .SetValue(data, configuration.GetSection("anim_to_play").ParseAnim() ?? animToPlay);

            service.Register(name, data);
            return new CardEffectDefinition(key, data, configuration);
        }
    }
}
