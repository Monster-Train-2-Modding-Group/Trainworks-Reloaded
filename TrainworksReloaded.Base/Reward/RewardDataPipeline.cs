using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Reward
{
    public class RewardDataPipeline : IDataPipeline<IRegister<RewardData>, RewardData>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;
        private readonly Dictionary<String, IFactory<RewardData>> generators;
        private readonly IModLogger<RewardDataPipeline> logger;

        public RewardDataPipeline(
            PluginAtlas atlas,
            IEnumerable<IFactory<RewardData>> generators,
            IGuidProvider guidProvider,
            IRegister<LocalizationTerm> termRegister,
            IModLogger<RewardDataPipeline> logger
        )
        {
            this.atlas = atlas;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
            this.generators = generators.ToDictionary(xs => xs.FactoryKey);
            this.logger = logger;
        }

        public List<IDefinition<RewardData>> Run(IRegister<RewardData> service)
        {
            var processList = new List<IDefinition<RewardData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadRewards(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        public List<RewardDataDefinition> LoadRewards(
            IRegister<RewardData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<RewardDataDefinition>();
            foreach (var child in pluginConfig.GetSection("rewards").GetChildren())
            {
                var data = LoadRewardConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public RewardDataDefinition? LoadRewardConfiguration(
            IRegister<RewardData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            RewardData? data = null;
            var type = configuration.GetSection("type").ParseString();
            if (type == null)
                return null;
            if (type != "custom_class")
            {
                if (!generators.TryGetValue(type, out var factory))
                {
                    return null;
                }
                data = factory.GetValue();
            }
            else
            {
                var classReference = configuration.GetSection("custom_class").ParseReference();
                if (classReference == null)
                {
                    logger.Log(LogLevel.Error, $"Custom class type specified for {id} but no custom_class specified. path: {configuration.GetPath()}");
                    return null;
                }
                var rewardClassName = classReference.id;
                var modReference = classReference.mod_reference ?? key;
                var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
                if (
                    !rewardClassName.GetFullyQualifiedName<RewardData>(
                        assembly,
                        out string? fullyQualifiedName
                    )
                )
                {
                    logger.Log(LogLevel.Error, $"Failed to load reward class {rewardClassName} in {id} mod {modReference}, Make sure the class exists in {modReference} and that the class inherits from RewardData.");
                    return null;
                }
                data = ScriptableObject.CreateInstance(fullyQualifiedName) as RewardData;
            }
            if (data == null)
                return null;

            var name = key.GetId(TemplateConstants.RewardData, id);
            data.name = name;

            string guid = guidProvider.GetGuidDeterministic(name).ToString();
            AccessTools.Field(typeof(RewardData), "id").SetValue(data, guid);

            var titleKey = $"RewardData_titleKey-{name}";
            var descriptionKey = $"RewardData_descriptionKey-{name}";

            //handle titles
            var localizationNameTerm = configuration.GetSection("titles").ParseLocalizationTerm();
            if (localizationNameTerm != null)
            {
                AccessTools.Field(typeof(RewardData), "_rewardTitleKey").SetValue(data, titleKey);
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
                    .Field(typeof(RewardData), "_rewardDescriptionKey")
                    .SetValue(data, descriptionKey);
                localizationDescTerm.Key = descriptionKey;
                termRegister.Register(descriptionKey, localizationDescTerm);
            }

            //boolean
            AccessTools
                .Field(typeof(RewardData), "_showRewardFlowInEvent")
                .SetValue(data, configuration.GetDeprecatedSection("show_in_event", "show_reward_flow_in_event").ParseBool() ?? false);

            AccessTools
                .Field(typeof(RewardData), "ShowRewardAnimationInEvent")
                .SetValue(
                    data,
                    configuration.GetSection("show_animation_in_event").ParseBool() ?? false
                );

            AccessTools
                .Field(typeof(RewardData), "_showCancelOverride")
                .SetValue(data, configuration.GetDeprecatedSection("show_cancel", "show_cancel_override").ParseBool() ?? false);

            AccessTools
                .Field(typeof(RewardData), "isUniqueInEndlessMode")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("endless_mode_unique", "is_unique_in_endless_mode").ParseBool() ?? false
                );

            //int[]
            var costs = configuration
                .GetSection("costs")
                .GetChildren()
                .Select(xs => xs.ParseInt() ?? 0)
                .ToList();
            AccessTools.Field(typeof(RewardData), "costs").SetValue(data, costs.ToArray());

            //filter
            AccessTools
                .Field(typeof(RewardData), "_filter")
                .SetValue(
                    data,
                    configuration.GetSection("filter").ParseRewardFilter() ?? RewardData.Filter.None
                );

            service.Register(name, data);

            return new RewardDataDefinition(key, data, configuration) { Id = id };
        }
    }
}
