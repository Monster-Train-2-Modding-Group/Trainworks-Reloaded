﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Reward;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Map
{
    public class MapNodePipeline : IDataPipeline<IRegister<MapNodeData>, MapNodeData>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly Dictionary<String, IFactory<MapNodeData>> generators;

        public MapNodePipeline(
            PluginAtlas atlas,
            IEnumerable<IFactory<MapNodeData>> generators,
            IRegister<LocalizationTerm> termRegister
        )
        {
            this.atlas = atlas;
            this.termRegister = termRegister;
            this.generators = generators.ToDictionary(xs => xs.FactoryKey);
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
            var type = configuration.GetSection("type").ParseString();
            if (type == null || !generators.TryGetValue(type, out var factory))
            {
                return null;
            }
            var data = factory.GetValue();
            if (data == null)
                return null;

            var name = key.GetId(TemplateConstants.MapNode, id);
            data.name = name;

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
                .SetValue(data, configuration.GetSection("is_banner").ParseBool() ?? false);

            AccessTools
                .Field(typeof(MapNodeData), "usePyreHeartHpTooltipKey")
                .SetValue(data, configuration.GetSection("use_hp_tooltip").ParseBool() ?? false);

            AccessTools
                .Field(typeof(MapNodeData), "updateMapIconImmediatelyOnClick")
                .SetValue(
                    data,
                    configuration.GetSection("updated_map_icon_on_click").ParseBool() ?? false
                );

            //string
            AccessTools
                .Field(typeof(MapNodeData), "nodeSelectedSfxCue")
                .SetValue(data, configuration.GetSection("node_selection_cue").ParseString() ?? "");

            //dlc
            AccessTools
                .Field(typeof(MapNodeData), "requiredDlc")
                .SetValue(data, configuration.GetSection("dlc").ParseDLC() ?? ShinyShoe.DLC.None);

            //skip settings
            AccessTools
                .Field(typeof(MapNodeData), "skipCheckSettings")
                .SetValue(data, configuration.GetSection("skip_settings").ParseSkipSettings() ?? 0);

            service.Register(name, data);

            return new MapNodeDefinition(key, data, configuration) { Id = id };
        }
    }
}
