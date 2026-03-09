using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Challenges
{
    public class ChallengeDataPipeline : IDataPipeline<IRegister<SpChallengeData>, SpChallengeData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<ChallengeDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;

        public ChallengeDataPipeline(
            PluginAtlas atlas,
            IModLogger<ChallengeDataPipeline> logger,
            IRegister<LocalizationTerm> termRegister,
            CardPoolRegister cardPoolRegister,
            IGuidProvider guidProvider
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
        }

        public List<IDefinition<SpChallengeData>> Run(IRegister<SpChallengeData> service)
        {
            var processList = new List<IDefinition<SpChallengeData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadItems(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<ChallengeDataDefinition> LoadItems(
            IRegister<SpChallengeData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<ChallengeDataDefinition>();
            foreach (var child in pluginConfig.GetSection("challenges").GetChildren())
            {
                var data = LoadConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private ChallengeDataDefinition? LoadConfiguration(
            IRegister<SpChallengeData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.Challenge, id);
            var titleKey = $"SpChallengeData_nameKey-{name}";
            var descriptionKey = $"SpChallengeData_descriptionKey-{name}";
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();

            string guid;
            if (overrideMode.IsOverriding() && service.TryLookupName(id, out SpChallengeData? data, out var _))
            {
                logger.Log(LogLevel.Info, $"Overriding Challenge {id}...");
                titleKey = data.GetNameKey();
                descriptionKey = data.GetDescriptionKey();
                guid = data.GetID();
            }
            else
            {
                data = ScriptableObject.CreateInstance<SpChallengeData>();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
            }
            AccessTools.Field(typeof(SpChallengeData), "id").SetValue(data, guid);

            var localizationTitleTerm = configuration.GetSection("names").ParseLocalizationTerm();
            if (localizationTitleTerm != null)
            {
                AccessTools.Field(typeof(SpChallengeData), "nameKey").SetValue(data, titleKey);
                localizationTitleTerm.Key = titleKey;
                termRegister.Register(titleKey, localizationTitleTerm);
            }

            var localizationDescriptionTerm = configuration
                .GetSection("descriptions")
                .ParseLocalizationTerm();
            if (localizationDescriptionTerm != null)
            {
                AccessTools
                    .Field(typeof(SpChallengeData), "descriptionKey")
                    .SetValue(data, descriptionKey);
                localizationDescriptionTerm.Key = descriptionKey;
                termRegister.Register(descriptionKey, localizationDescriptionTerm);
            }

            var mainChampionIndex = data.GetMainChampionIndex();
            AccessTools
                .Field(typeof(SpChallengeData), "mainChampionIndex")
                .SetValue(
                    data,
                    configuration.GetSection("main_champion_index").ParseInt() ?? mainChampionIndex
                );

            var alliedChampionIndex = data.GetAlliedChampionIndex();
            AccessTools
                .Field(typeof(SpChallengeData), "alliedChampionIndex")
                .SetValue(
                    data,
                    configuration.GetSection("allied_champion_index").ParseInt() ?? alliedChampionIndex
                );

            var covenantLevel = data.GetCovenantLevel();
            AccessTools
                .Field(typeof(SpChallengeData), "covenantLevel")
                .SetValue(data, configuration.GetSection("covenant_level").ParseInt() ?? covenantLevel);

            var modded = overrideMode.IsNewContent();
            if (modded)
            {
                service.Register(name, data);
            }

            return new ChallengeDataDefinition(key, data, configuration, !modded)
            {
                Id = id
            };
        }
    }
}
