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

namespace TrainworksReloaded.Base.Relic
{
    public class RelicDataPipeline : IDataPipeline<IRegister<RelicData>, RelicData>
    {
        private readonly PluginAtlas _atlas;
        private readonly IModLogger<RelicDataPipeline> _logger;
        private readonly IRegister<LocalizationTerm> _localizationRegister;
        private readonly Dictionary<String, IFactory<RelicData>> generators;
        private readonly IGuidProvider _guidProvider;


        public RelicDataPipeline(
            PluginAtlas atlas,
            IModLogger<RelicDataPipeline> logger,
            IRegister<LocalizationTerm> localizationRegister,
            IEnumerable<IFactory<RelicData>> generators,
            IGuidProvider guidProvider
        )
        {
            _atlas = atlas;
            _logger = logger;
            _localizationRegister = localizationRegister;
            this.generators = generators.ToDictionary(xs => xs.FactoryKey);
            _guidProvider = guidProvider;
        }

        public List<IDefinition<RelicData>> Run(IRegister<RelicData> register)
        {
            var processList = new List<IDefinition<RelicData>>();
            foreach (var config in _atlas.PluginDefinitions)
            {
                processList.AddRange(LoadRelics(register, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<RelicDataDefinition> LoadRelics(
            IRegister<RelicData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<RelicDataDefinition>();
            foreach (var child in pluginConfig.GetSection("relics").GetChildren())
            {
                var data = LoadRelicConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private RelicDataDefinition? LoadRelicConfiguration(
            IRegister<RelicData> service,
            string key,
            IConfigurationSection config
        )
        {
            var relicId = config.GetSection("id").ParseString();
            if (string.IsNullOrEmpty(relicId))
            {
                _logger.Log(LogLevel.Error, $"Relic configuration missing required 'id' field");
                return null;
            }

            var name = key.GetId(TemplateConstants.RelicData, relicId);
            var type = config.GetSection("type").ParseString();
            if (type == null || !generators.TryGetValue(type, out var factory))
            {
                _logger.Log(LogLevel.Error, $"Generator for relic type {type} missing");
                return null;
            }
            var data = factory.GetValue();
            if (data == null)
            {
                _logger.Log(LogLevel.Error, $"Factory for relic type {type} missing");
                return null;
            }
            data.name = name;
            var guid = _guidProvider.GetGuidDeterministic(name);
            AccessTools.Field(typeof(RelicData), "id").SetValue(data, guid.ToString());

            // Create localization keys
            var nameKey = $"RelicData_titleKey-{name}";
            var descriptionKey = $"RelicData_descriptionKey-{name}";
            var activatedKey = $"RelicData_activatedKey-{name}";

            // Handle name localization
            var nameTerm = config.GetSection("names").ParseLocalizationTerm();
            if (nameTerm != null)
            {
                AccessTools.Field(typeof(RelicData), "nameKey").SetValue(data, nameKey);
                nameTerm.Key = nameKey;
                _localizationRegister.Register(nameKey, nameTerm);
            }

            // Handle description localization
            var descriptionTerm = config.GetSection("descriptions").ParseLocalizationTerm();
            if (descriptionTerm != null)
            {
                AccessTools.Field(typeof(RelicData), "descriptionKey").SetValue(data, descriptionKey);
                descriptionTerm.Key = descriptionKey;
                _localizationRegister.Register(descriptionKey, descriptionTerm);
            }

            // Handle relic activation localization
            var activatedTerm = config.GetSection("relic_activated_texts").ParseLocalizationTerm();
            if (activatedTerm != null)
            {
                AccessTools.Field(typeof(RelicData), "relicActivatedKey").SetValue(data, activatedKey);
                activatedTerm.Key = activatedKey;
                _localizationRegister.Register(activatedKey, activatedTerm);
            }

            // Handle lore tooltips
            var loreTooltips = new List<string>();
            var loreTooltipsSection = config.GetSection("lore_tooltips");
            if (loreTooltipsSection.Exists())
            {
                var tooltips = loreTooltipsSection.GetChildren().ToList();
                for (int i = 0; i < tooltips.Count; i++)
                {
                    var tooltip = tooltips[i];
                    var tooltipKey = $"RelicData_loreTooltipKey-{name}-{i}";
                    var tooltipTerm = tooltip.ParseLocalizationTerm();
                    if (tooltipTerm != null)
                    {
                        tooltipTerm.Key = tooltipKey;
                        _localizationRegister.Register(tooltipKey, tooltipTerm);
                        loreTooltips.Add(tooltipKey);
                    }
                }
            }
            AccessTools.Field(typeof(RelicData), "relicLoreTooltipKeys").SetValue(data, loreTooltips);

            // Handle deployment phase restriction
            var disallowInDeploymentPhase = config.GetSection("disallow_in_deployment").ParseBool();
            AccessTools.Field(typeof(RelicData), "disallowInDeploymentPhase").SetValue(data, disallowInDeploymentPhase);

            // Handle lore tooltip style
            var loreStyle = config.GetSection("lore_style").ParseRelicLoreTooltipStyle();
            AccessTools.Field(typeof(RelicData), "relicLoreTooltipStyle").SetValue(data, loreStyle);

            service.Register(name, data);
            return new RelicDataDefinition(key, data, config)
            {
                Id = relicId
            };
        }
    }
}