using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Pyre
{
    public class PyreHeartDataPipeline : IDataPipeline<IRegister<PyreHeartData>, PyreHeartData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<PyreHeartDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> localizationService;

        public PyreHeartDataPipeline(
            PluginAtlas atlas,
            IModLogger<PyreHeartDataPipeline> logger,
            IRegister<LocalizationTerm> localizationService
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.localizationService = localizationService;
        }

        public List<IDefinition<PyreHeartData>> Run(IRegister<PyreHeartData> service)
        {
            var processList = new List<IDefinition<PyreHeartData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadItems(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<PyreHeartDefinition> LoadItems(IRegister<PyreHeartData> service, string key, IConfiguration pluginConfig)
        {
            var processList = new List<PyreHeartDefinition>();
            foreach (var child in pluginConfig.GetSection("pyre_hearts").GetChildren())
            {
                var data = LoadConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private PyreHeartDefinition? LoadConfiguration(IRegister<PyreHeartData> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId(TemplateConstants.PyreHeart, id);
            var data = new PyreHeartData();

            var activatedText = configuration.GetSection("activated_texts").ParseLocalizationTerm();
            if (activatedText != null)
            {
                var activatedKey = $"PyreHeartData_activatedText-{name}";
                activatedText.Key = activatedKey;
                localizationService.Register(activatedText.Key, activatedText);
                AccessTools.Field(typeof(PyreHeartData), "activatedKey").SetValue(data, activatedKey);
            }

            var hp = configuration.GetSection("hp").ParseInt() ?? data.GetStartingHP();
            AccessTools.Field(typeof(PyreHeartData), "startingHP").SetValue(data, hp);

            var attack = configuration.GetSection("attack").ParseInt() ?? data.GetAttack();
            AccessTools.Field(typeof(PyreHeartData), "attack").SetValue(data, attack);

            var upgradeHp = configuration.GetSection("upgrade_hp").GetChildren().Select(x => x.ParseInt()).Where(x => x != null).Cast<int>().ToArray();
            AccessTools.Field(typeof(PyreHeartData), "upgradeHP").SetValue(data, upgradeHp);

            var upgradeAttack = configuration.GetSection("upgrade_attack").GetChildren().Select(x => x.ParseInt()).Where(x => x != null).Cast<int>().ToArray();
            AccessTools.Field(typeof(PyreHeartData), "upgradeAttack").SetValue(data, upgradeAttack);

            var color = configuration.GetSection("body_color").ParseColor() ?? data.GetBodyColor();
            AccessTools.Field(typeof(PyreHeartData), "bodyColor").SetValue(data, color);

            var criteriaConfig = configuration.GetSection("unlock_criteria");
            if (criteriaConfig.Exists())
            {
                var unlockCriteria = data.GetUnlockData();
                ParseCriteria(name, unlockCriteria, criteriaConfig);
            }

            service.Register(name, data);
            return new PyreHeartDefinition(key, data, configuration) { Id = id };
        }

        private void ParseCriteria(string name, UnlockCriteria data, IConfigurationSection configuration)
        {
            var paramInt = configuration.GetSection("param_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(UnlockCriteria), "paramInt").SetValue(data, paramInt);

            var unlockCondition = configuration.GetSection("condition").ParseTrackedValue() ?? MetagameSaveData.TrackedValue.None;
            AccessTools.Field(typeof(UnlockCriteria), "unlockCondition").SetValue(data, unlockCondition);

            var unlockDescription = configuration.GetSection("descriptions").ParseLocalizationTerm();
            if (unlockDescription != null)
            {
                var descriptionKey = $"PyreHeartData_unlockDescription-{name}";
                unlockDescription.Key = descriptionKey;
                localizationService.Register(unlockDescription.Key, unlockDescription);
                AccessTools.Field(typeof(UnlockCriteria), "descriptionKey").SetValue(data, descriptionKey);
            }
        }
    }
}
