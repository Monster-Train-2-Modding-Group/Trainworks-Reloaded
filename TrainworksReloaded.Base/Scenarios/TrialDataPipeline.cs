using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trials
{
    public class TrialDataPipeline : IDataPipeline<IRegister<TrialData>, TrialData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<TrialDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;

        public TrialDataPipeline(
            PluginAtlas atlas,
            IModLogger<TrialDataPipeline> logger,
            IRegister<LocalizationTerm> termRegister,
            IGuidProvider guidProvider
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
        }

        public List<IDefinition<TrialData>> Run(IRegister<TrialData> service)
        {
            var processList = new List<IDefinition<TrialData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadTrials(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private IEnumerable<IDefinition<TrialData>> LoadTrials(IRegister<TrialData> service, string key, IConfiguration configuration)
        {
            var processList = new List<TrialDataDefinition>();
            foreach (var child in configuration.GetSection("trials").GetChildren())
            {
                var data = LoadConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private TrialDataDefinition? LoadConfiguration(IRegister<TrialData> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.Trial, id);

            var data = new TrialData();

            data.name = name;
            var guid = guidProvider.GetGuidDeterministic(name);
            AccessTools.Field(typeof(TrialData), "id").SetValue(data, guid.ToString());

            service.Register(name, data);

            return new TrialDataDefinition(key, data, configuration)
            {
                Id = id,
                IsModded = true,
            };
        }
    }
}
