using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class CovenantDataPipelineDecorator : IDataPipeline<IRegister<RelicData>, RelicData>
    {
        private readonly IModLogger<CovenantDataPipelineDecorator> logger;
        private readonly IDataPipeline<IRegister<RelicData>, RelicData> decoratee;

        public CovenantDataPipelineDecorator(
            IModLogger<CovenantDataPipelineDecorator> logger,
            IDataPipeline<IRegister<RelicData>, RelicData> decoratee
        )
        {
            this.logger = logger;
            this.decoratee = decoratee;
        }

        public List<IDefinition<RelicData>> Run(IRegister<RelicData> register)
        {
            var definitions = decoratee.Run(register);
            foreach (var definition in definitions)
            {
                ProcessData((definition as RelicDataDefinition)!);
            }
            return definitions;
        }

        private void ProcessData(RelicDataDefinition definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var overrideMode = definition.Override;
            var key = definition.Key;
            var relicId = definition.Id.ToId(key, TemplateConstants.RelicData);

            if (data is not CovenantData covenant)
                return;

            if (definition.CopyData is not CovenantData copyData)
                copyData = covenant;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("covenant").Exists())
                .Select(xs => xs.GetSection("covenant"))
                .FirstOrDefault();

            if (configuration == null)
                return;

            var unlockLevel = configuration.GetSection("unlock_level").ParseInt() ?? copyData.GetUnlockLevel();
            AccessTools.Field(typeof(CovenantData), "unlockLevel").SetValue(covenant, unlockLevel);

            var ascension_level = configuration.GetSection("ascension_level").ParseInt() ?? copyData.AscensionLevel;
            AccessTools.Field(typeof(CovenantData), "ascensionLevel").SetValue(covenant, ascension_level);

            var disables_covenant = configuration.GetSection("disables_covenant").Value ?? copyData.DisablesCovenant;
            AccessTools.Field(typeof(CovenantData), "disablesCovenant").SetValue(covenant, disables_covenant);
            
        }
    }
}