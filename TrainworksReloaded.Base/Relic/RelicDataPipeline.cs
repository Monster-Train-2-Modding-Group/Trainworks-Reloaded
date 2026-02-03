using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicDataPipeline : IDataPipeline<IRegister<RelicData>, RelicData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<RelicDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> localizationRegister;
        private readonly Dictionary<String, IFactory<RelicData>> generators;
        private readonly IGuidProvider guidProvider;


        public RelicDataPipeline(
            PluginAtlas atlas,
            IModLogger<RelicDataPipeline> logger,
            IRegister<LocalizationTerm> localizationRegister,
            IEnumerable<IFactory<RelicData>> generators,
            IGuidProvider guidProvider
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.localizationRegister = localizationRegister;
            this.generators = generators.ToDictionary(xs => xs.FactoryKey);
            this.guidProvider = guidProvider;
        }

        public List<IDefinition<RelicData>> Run(IRegister<RelicData> register)
        {
            var processList = new List<IDefinition<RelicData>>();
            foreach (var config in atlas.PluginDefinitions)
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
            var id = config.GetSection("id").ParseString();
            if (string.IsNullOrEmpty(id))
            {
                logger.Log(LogLevel.Error, $"Relic configuration missing required 'id' field");
                return null;
            }

            var name = key.GetId(TemplateConstants.RelicData, id);
            RelicData? data;
            RelicData? copyData;

            // Create localization keys
            var nameKey = $"RelicData_titleKey-{name}";
            var descriptionKey = $"RelicData_descriptionKey-{name}";
            var activatedKey = $"RelicData_activatedKey-{name}";
            var overrideMode = config.GetSection("override").ParseOverrideMode();
            var cloneId = config.GetSection("clone_id").Value;

            var type = config.GetSection("type").ParseString();
            if (type == null || !generators.TryGetValue(type, out var factory))
            {
                logger.Log(LogLevel.Error, $"Generator for relic type {type} missing");
                return null;
            }

            if (cloneId != null)
            {
                logger.Log(LogLevel.Debug, $"Cloning RelicData {cloneId}...");
                service.TryLookupName(cloneId, out var cloneData, out var _);
                data = factory.GetValue();
                if (data == null)
                {
                    logger.Log(LogLevel.Error, $"Factory for relic type {type} missing");
                    return null;
                }
                data.name = name;
                var guid = guidProvider.GetGuidDeterministic(name).ToString();
                AccessTools.Field(typeof(RelicData), "id").SetValue(data, guid);
                copyData = cloneData ?? data;
            }
            else if (overrideMode.IsOverriding() && service.TryLookupName(id, out data, out var _))
            {
                logger.Log(LogLevel.Info, $"Overriding RelicData {id}... ");
                descriptionKey = data.GetDescriptionKey();
                activatedKey = data.GetRelicActivatedKey();
                nameKey = data.GetNameKey();
                copyData = data;
            }
            else
            {
                data = factory.GetValue();
                if (data == null)
                {
                    logger.Log(LogLevel.Error, $"Factory for relic type {type} missing");
                    return null;
                }
                data.name = name;
                var guid = guidProvider.GetGuidDeterministic(name).ToString();
                AccessTools.Field(typeof(RelicData), "id").SetValue(data, guid);
                copyData = data;
            }

            // Handle name localization
            AccessTools.Field(typeof(RelicData), "nameKey").SetValue(data, copyData.GetNameKey());
            var nameTerm = config.GetSection("names").ParseLocalizationTerm();
            if (nameTerm != null)
            {
                AccessTools.Field(typeof(RelicData), "nameKey").SetValue(data, nameKey);
                nameTerm.Key = nameKey;
                localizationRegister.Register(nameKey, nameTerm);
            }

            // Handle description localization
            var descriptionTerm = config.GetSection("descriptions").ParseLocalizationTerm();
            AccessTools.Field(typeof(RelicData), "descriptionKey").SetValue(data, copyData.GetDescriptionKey());
            if (descriptionTerm != null)
            {
                AccessTools.Field(typeof(RelicData), "descriptionKey").SetValue(data, descriptionKey);
                descriptionTerm.Key = descriptionKey;
                localizationRegister.Register(descriptionKey, descriptionTerm);
            }

            // Handle relic activation localization
            var activatedTerm = config.GetSection("relic_activated_texts").ParseLocalizationTerm();
            AccessTools.Field(typeof(RelicData), "relicActivatedKey").SetValue(data, copyData.GetRelicActivatedKey());
            if (activatedTerm != null)
            {
                AccessTools.Field(typeof(RelicData), "relicActivatedKey").SetValue(data, activatedKey);
                activatedTerm.Key = activatedKey;
                localizationRegister.Register(activatedKey, activatedTerm);
            }

            // Handle lore tooltips
            var loreTooltips = copyData.GetRelicLoreTooltipKeys() ?? [];
            if (copyData != data)
                loreTooltips = [.. loreTooltips];
            AccessTools.Field(typeof(RelicData), "relicLoreTooltipKeys").SetValue(data, loreTooltips);
            var loreTooltipsSection = config.GetSection("lore_tooltips");
            if (overrideMode == OverrideMode.Replace && loreTooltipsSection.Exists())
            {
                loreTooltips.Clear();
            }
            int tooltip_count = loreTooltips.Count;
            foreach (var tooltip in loreTooltipsSection.GetChildren())
            {
                var localizationTooltipTerm = tooltip.ParseLocalizationTerm();
                if (localizationTooltipTerm != null)
                {
                    var tooltipKey = $"RelicData_loreTooltipKey-{tooltip_count}-{name}";
                    if (localizationTooltipTerm.Key.IsNullOrEmpty())
                        localizationTooltipTerm.Key = tooltipKey;
                    loreTooltips.Add(tooltipKey);
                    if (localizationTooltipTerm.HasTranslation())
                        localizationRegister.Register(tooltipKey, localizationTooltipTerm);
                    tooltip_count++;
                }
            }

            // Handle deployment phase restriction
            var disallowInDeploymentPhase = config.GetSection("disallow_in_deployment").ParseBool() ?? copyData.DisallowInDeploymentPhase;
            AccessTools.Field(typeof(RelicData), "disallowInDeploymentPhase").SetValue(data, disallowInDeploymentPhase);

            // Handle lore tooltip style
            var loreStyle = config.GetSection("lore_style").ParseRelicLoreTooltipStyle() ?? copyData.GetRelicLoreTooltipStyle();
            AccessTools.Field(typeof(RelicData), "relicLoreTooltipStyle").SetValue(data, loreStyle);

            service.Register(name, data);
            return new RelicDataDefinition(key, data, copyData, overrideMode, config)
            {
                Id = id
            };
        }
    }
}