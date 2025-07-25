using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Scenarios
{
    public class ScenarioPipeline : IDataPipeline<IRegister<ScenarioData>, ScenarioData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<ScenarioPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;
        private readonly ScenarioDelegator delegator;

        public ScenarioPipeline(
            PluginAtlas atlas,
            IModLogger<ScenarioPipeline> logger,
            IRegister<LocalizationTerm> termRegister,
            IGuidProvider guidProvider,
            ScenarioDelegator delegator
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
            this.delegator = delegator;
        }

        public List<IDefinition<ScenarioData>> Run(IRegister<ScenarioData> service)
        {
            var processList = new List<IDefinition<ScenarioData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadScenarios(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private IEnumerable<IDefinition<ScenarioData>> LoadScenarios(IRegister<ScenarioData> service, string key, IConfiguration configuration)
        {
            var processList = new List<ScenarioDefinition>();
            foreach (var child in configuration.GetSection("scenarios").GetChildren())
            {
                var data = LoadConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private ScenarioDefinition? LoadConfiguration(IRegister<ScenarioData> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.Scenario, id);
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();
            var cloneId = configuration.GetSection("clone_id").Value;

            var nameKey = $"ScenarioData_titleKey-{name}";
            var descriptionKey = $"ScenarioData_descriptionKey-{name}";

            string guid;
            ScenarioData copyData;
            ScenarioData data;
            if (cloneId != null)
            {
                logger.Log(LogLevel.Debug, $"Cloning Scenario {cloneId}...");
                service.TryLookupName(cloneId, out var cloneData, out var _);
                data = ScriptableObject.CreateInstance<ScenarioData>();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
                copyData = cloneData ?? data;
            }
            else if (overrideMode.IsOverriding() && service.TryLookupName(id, out data!, out var _))
            {
                logger.Log(LogLevel.Info, $"Overriding Scenario {id}...");
                guid = data.GetID();
                copyData = data;
            }
            else
            {
                data = ScriptableObject.CreateInstance<ScenarioData>();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
                copyData = data;
            }

            AccessTools.Field(typeof(ScenarioData), "id").SetValue(data, guid.ToString());

            var distance = configuration.GetSection("distance").ParseInt();
            var runType = configuration.GetSection("run_type").ParseString() ?? "primary";
            if (distance != null)
            {
                delegator.Add(data, distance.Value, runType);
            }

            var battleTrack = copyData.GetBattleTrackNameData() ?? "";
            AccessTools.Field(typeof(ScenarioData), "battleTrackNameData")
                .SetValue(data, configuration.GetSection("battle_track_name").ParseString() ?? battleTrack);

            var bossSpawnSFXCue = copyData.GetBossSpawnSFXCue() ?? "";
            AccessTools.Field(typeof(ScenarioData), "bossSpawnSFXCue")
                .SetValue(data, configuration.GetSection("boss_spawn_sfx_cue").ParseString() ?? bossSpawnSFXCue);

            var minTreasureUnits = copyData.GetMinTreasureUnits();
            AccessTools.Field(typeof(ScenarioData), "minTreasureUnits")
                .SetValue(data, configuration.GetSection("min_treasure_units").ParseInt() ?? minTreasureUnits);

            var maxTreasureUnits = copyData.GetMaxTreasureUnits();
            AccessTools.Field(typeof(ScenarioData), "maxTreasureUnits")
                .SetValue(data, configuration.GetSection("max_treasure_units").ParseInt() ?? maxTreasureUnits);

            var startingEnergy = copyData.GetStartingEnergy();
            AccessTools.Field(typeof(ScenarioData), "startingEnergy")
                .SetValue(data, configuration.GetSection("starting_energy").ParseInt() ?? startingEnergy);

            var difficulty = copyData.GetDifficulty();
            AccessTools.Field(typeof(ScenarioData), "difficulty")
                .SetValue(data, configuration.GetSection("difficulty").ParseScenarioDifficulty() ?? difficulty);

            // Handle name localization
            var nameTerm = configuration.GetSection("names").ParseLocalizationTerm();
            if (nameTerm != null)
            {
                if (nameTerm.Key.IsNullOrEmpty())
                    nameTerm.Key = nameKey;
                if (nameTerm.HasTranslation())
                    termRegister.Register(nameTerm.Key, nameTerm);
                AccessTools.Field(typeof(ScenarioData), "battleNameKey").SetValue(data, nameTerm.Key);
            }
            else
            {
                var battleNameKey = AccessTools.Field(typeof(ScenarioData), "battleNameKey").GetValue(copyData);
                AccessTools.Field(typeof(ScenarioData), "battleNameKey").SetValue(data, battleNameKey);
            }

            // Handle description localization
            var descriptionTerm = configuration.GetSection("descriptions").ParseLocalizationTerm();
            if (descriptionTerm != null)
            {
                if (descriptionTerm.Key.IsNullOrEmpty())
                    descriptionTerm.Key = descriptionKey;
                if (descriptionTerm.HasTranslation())
                    termRegister.Register(descriptionTerm.Key, descriptionTerm);
                AccessTools.Field(typeof(ScenarioData), "battleDescriptionKey").SetValue(data, descriptionTerm.Key);
            }
            else
            {
                var battleDescriptionKey = AccessTools.Field(typeof(ScenarioData), "battleDescriptionKey").GetValue(copyData);
                AccessTools.Field(typeof(ScenarioData), "battleDescriptionKey").SetValue(data, battleDescriptionKey);
            }

            var isModded = overrideMode.IsNewContent() || cloneId != null;
            if (isModded)
                service.Register(name, data);

            return new ScenarioDefinition(key, data, copyData, overrideMode, configuration)
            {
                Id = id,
                IsModded = isModded,
            };
        }
    }
}
