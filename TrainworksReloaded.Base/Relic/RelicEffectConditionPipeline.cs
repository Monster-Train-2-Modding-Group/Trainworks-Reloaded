using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectConditionPipeline : IDataPipeline<IRegister<RelicEffectCondition>, RelicEffectCondition>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<RelicEffectConditionPipeline> logger;

        public RelicEffectConditionPipeline(
            PluginAtlas atlas,
            IModLogger<RelicEffectConditionPipeline> logger
        )
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<RelicEffectCondition>> Run(IRegister<RelicEffectCondition> register)
        {
            var processList = new List<IDefinition<RelicEffectCondition>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadRelicEffectConditions(register, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<RelicEffectConditionDefinition> LoadRelicEffectConditions(
            IRegister<RelicEffectCondition> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<RelicEffectConditionDefinition>();
            foreach (var child in pluginConfig.GetSection("relic_effect_conditions").GetChildren())
            {
                var data = LoadRelicEffectCondition(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private RelicEffectConditionDefinition? LoadRelicEffectCondition(
            IRegister<RelicEffectCondition> service,
            string key,
            IConfigurationSection configuration
        )
        {
            var internalID = configuration.GetSection("id").ParseString();
            if (string.IsNullOrEmpty(internalID))
            {
                logger.Log(LogLevel.Error, $"Relic effect condition configuration missing required 'id' field");
                return null;
            }

            var name = key.GetId(TemplateConstants.RelicEffectCondition, internalID);
            var data = new RelicEffectCondition();

            var paramTrackedValue = CardStatistics.TrackedValueType.SubtypeInDeck;
            AccessTools
                .Field(typeof(RelicEffectCondition), "paramTrackedValue")
                .SetValue(data, configuration.GetSection("param_tracked_value").ParseTrackedValueType() ?? paramTrackedValue);

            var paramCardType = CardStatistics.CardTypeTarget.Any;
            AccessTools
                .Field(typeof(RelicEffectCondition), "paramCardType")
                .SetValue(data, configuration.GetSection("param_card_type").ParseCardTypeTarget() ?? paramCardType);

            var paramEntryDuration = CardStatistics.EntryDuration.ThisTurn;
            AccessTools
                .Field(typeof(RelicEffectCondition), "paramEntryDuration")
                .SetValue(data, configuration.GetSection("param_entry_duration").ParseEntryDuration() ?? paramEntryDuration);

            AccessTools
                .Field(typeof(RelicEffectCondition), "paramTrackTriggerCount")
                .SetValue(data, configuration.GetSection("param_track_trigger_count").ParseBool() ?? false);

            var paramComparator = RelicEffectCondition.Comparator.Equal | RelicEffectCondition.Comparator.GreaterThan;
            AccessTools
                .Field(typeof(RelicEffectCondition), "paramComparator")
                .SetValue(data, configuration.GetSection("param_comparator").ParseComparator() ?? paramComparator);

            AccessTools
                .Field(typeof(RelicEffectCondition), "paramInt")
                .SetValue(data, configuration.GetSection("param_int").ParseInt() ?? 0);

            AccessTools
                .Field(typeof(RelicEffectCondition), "allowMultipleTriggersPerDuration")
                .SetValue(data, configuration.GetSection("allow_multiple_triggers_per_duration").ParseBool() ?? true);


            service.Register(name, data);
            return new RelicEffectConditionDefinition(key, data, configuration){
                Id = internalID
            };
        }
    }
}