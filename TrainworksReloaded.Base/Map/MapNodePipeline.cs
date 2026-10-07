using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Reward;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Map
{
    public class MapNodePipeline : IDataPipeline<IRegister<MapNodeData>, MapNodeData>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly Dictionary<String, IFactory<MapNodeData>> generators;
        private readonly IGuidProvider guidProvider;
        private readonly IModLogger<RewardDataPipeline> logger;

        public MapNodePipeline(
            PluginAtlas atlas,
            IEnumerable<IFactory<MapNodeData>> generators,
            IRegister<LocalizationTerm> termRegister,
            IGuidProvider guidProvider,
            IModLogger<RewardDataPipeline> logger
        )
        {
            this.atlas = atlas;
            this.termRegister = termRegister;
            this.generators = generators.ToDictionary(xs => xs.FactoryKey);
            this.guidProvider = guidProvider;
            this.logger = logger;
        }

        public List<IDefinition<MapNodeData>> Run(IRegister<MapNodeData> service)
        {
            var processList = new List<IDefinition<MapNodeData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadMapNodes(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        public List<MapNodeDefinition> LoadMapNodes(
            IRegister<MapNodeData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<MapNodeDefinition>();
            foreach (var child in pluginConfig.GetSection("map_nodes").GetChildren())
            {
                var data = LoadMapNodeConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public MapNodeDefinition? LoadMapNodeConfiguration(
            IRegister<MapNodeData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            MapNodeData? data;
            var type = configuration.GetSection("type").ParseString();
            if (type == "custom_class")
            {
                var classReference = configuration.GetSection("custom_class").ParseReference();
                if (classReference == null)
                {
                    logger.Log(LogLevel.Error, $"Custom class type specified for {id} but no custom_class specified. path: {configuration.GetPath()}");
                    return null;
                }
                var mapNodeClassName = classReference.id;
                var modReference = classReference.mod_reference ?? key;
                var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
                if (
                    !mapNodeClassName.FindTypeFromClassName<MapNodeData>(
                        assembly,
                        out Type? foundType
                    )
                )
                {
                    logger.Log(LogLevel.Error, $"Failed to load map node class {mapNodeClassName} in {id} mod {modReference}, Make sure the class exists in {modReference} and that the class inherits from MapNodeData.");
                    return null;
                }
                data = ScriptableObject.CreateInstance(foundType) as MapNodeData;
            }
            else 
            {
                if (type == null || !generators.TryGetValue(type, out var factory))
                    return null;
                data = factory.GetValue();
            }

            if (data == null)
                return null;

            var name = key.GetId(TemplateConstants.MapNode, id);
            data.name = name;

            //handle id
            var guid = guidProvider.GetGuidDeterministic(name).ToString();
            AccessTools.Field(typeof(MapNodeData), "id").SetValue(data, guid);

            var titleKey = $"MapNode_titleKey-{name}";
            var descriptionKey = $"MapNode_descriptionKey-{name}";

            //handle titles
            var localizationNameTerm = configuration.GetSection("titles").ParseLocalizationTerm();
            if (localizationNameTerm != null)
            {
                AccessTools.Field(typeof(MapNodeData), "tooltipTitleKey").SetValue(data, titleKey);
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
                    .Field(typeof(MapNodeData), "tooltipBodyKey")
                    .SetValue(data, descriptionKey);
                localizationDescTerm.Key = descriptionKey;
                termRegister.Register(descriptionKey, localizationDescTerm);
            }

            //boolean
            AccessTools
                .Field(typeof(MapNodeData), "isBannerNode")
                .SetValue(data, configuration.GetDeprecatedSection("is_banner", "is_banner_node").ParseBool() ?? false);

            AccessTools
                .Field(typeof(MapNodeData), "isSoulSaviorUpgradedNode")
                .SetValue(data, configuration.GetSection("is_soul_savior_upgraded_node").ParseBool() ?? false);

            AccessTools
                .Field(typeof(MapNodeData), "usePyreHeartHpTooltipKey")
                .SetValue(data, configuration.GetDeprecatedSection("use_hp_tooltip", "use_pyre_hp_tooltip").ParseBool() ?? false);

            AccessTools
                .Field(typeof(MapNodeData), "updateMapIconImmediatelyOnClick")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("updated_map_icon_on_click", "update_map_icon_immediately_on_click").ParseBool() ?? false
                );

            //dlc
            AccessTools
                .Field(typeof(MapNodeData), "requiredDlc")
                .SetValue(data, configuration.GetDeprecatedSection("dlc", "required_dlc").ParseDLC() ?? ShinyShoe.DLC.None);

            //skip settings
            AccessTools
                .Field(typeof(MapNodeData), "skipCheckSettings")
                .SetValue(data, configuration.GetDeprecatedSection("skip_settings", "skip_check_settings").ParseSkipSettings() ?? 0);

            service.Register(name, data);

            return new MapNodeDefinition(key, data, configuration) { Id = id };
        }
    }
}
