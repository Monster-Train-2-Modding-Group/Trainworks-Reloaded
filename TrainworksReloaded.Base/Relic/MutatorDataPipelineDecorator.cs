using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class MutatorDataPipelineDecorator : IDataPipeline<IRegister<RelicData>, RelicData>
    {
        private readonly IModLogger<MutatorDataPipelineDecorator> logger;
        private readonly IDataPipeline<IRegister<RelicData>, RelicData> decoratee;

        private readonly FieldInfo BoonValueField = AccessTools.Field(typeof(MutatorData), "boonValue");
        private readonly FieldInfo DisableInDailyChallengesField = AccessTools.Field(typeof(MutatorData), "disableInDailyChallenges");
        private readonly FieldInfo RequiredDLCField = AccessTools.Field(typeof(MutatorData), "requiredDLC");
        private readonly FieldInfo TagsField = AccessTools.Field(typeof(MutatorData), "tags");

        public MutatorDataPipelineDecorator(IModLogger<MutatorDataPipelineDecorator> logger, IDataPipeline<IRegister<RelicData>, RelicData> decoratee)
        {
            this.logger = logger;
            this.decoratee = decoratee;
        }

        public List<IDefinition<RelicData>> Run(IRegister<RelicData> register)
        {
            var definitions = decoratee.Run(register);
            foreach (var definition in definitions)
            {
                ProcessMutatorData(definition);
            }
            return definitions;
        }

        private void ProcessMutatorData(IDefinition<RelicData> definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            var relicId = definition.Id.ToId(key, TemplateConstants.RelicData);

            if (data is not MutatorData mutator)
                return;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("mutator").Exists())
                .Select(xs => xs.GetSection("mutator"))
                .FirstOrDefault();

            if (configuration == null)
                return;

            logger.Log(LogLevel.Debug, $"Processing Mutator Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            int boonValue = configuration.GetSection("boon_value").ParseInt() ?? mutator.GetBoonValue();
            BoonValueField.SetValue(mutator, boonValue);

            bool disabledInDailies = configuration.GetSection("disable_in_daily_challenges").ParseBool() ?? mutator.GetDisableInDailyChallenges();
            DisableInDailyChallengesField.SetValue(mutator, disabledInDailies);

            var dlc = configuration.GetSection("required_dlc").ParseDLC() ?? mutator.GetRequiredDLC();
            RequiredDLCField.SetValue(mutator, dlc);

            var tags = configuration.GetSection("tags").GetChildren().Select(x => x.ParseString()).Where(x => x != null).Cast<string>().ToList();
            TagsField.SetValue(mutator, tags);
        }
    }
}