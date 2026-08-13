using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Reward
{
    public class RelicDraftRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<RelicDraftRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IRegister<RelicPool> relicPoolRegister;
        private readonly IDataFinalizer decoratee;

        public RelicDraftRewardDataFinalizerDecorator(
            IModLogger<RelicDraftRewardDataFinalizerDecorator> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<RelicPool> relicPoolRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.relicPoolRegister = relicPoolRegister;
            this.decoratee = decoratee;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRewardData(definition);
            }
            decoratee.FinalizeData();
            cache.Clear();
        }

        private void FinalizeRewardData(IDefinition<RewardData> definition)
        {
            var configuration1 = definition.Configuration;
            var data1 = definition.Data;
            var key = definition.Key;

            if (data1 is not RelicDraftRewardData data)
                return;

            var configuration = configuration1.GetExtension("relic_draft");

            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Relic Draft Reward Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var relicPoolReference = configuration.GetSection("draft_pool").ParseReference();
            if (
                relicPoolReference != null
                && relicPoolRegister.TryLookupName(
                    relicPoolReference.ToId(key, TemplateConstants.RelicPool),
                    out var lookup,
                    out var _,
                    relicPoolReference.context
                )
            )
            {
                AccessTools.Field(typeof(RelicDraftRewardData), "draftPool").SetValue(data, lookup);
            }

            AccessTools.Field(typeof(RelicDraftRewardData), "randomizeOrder").SetValue(data, configuration.GetSection("randomize_order").ParseBool() ?? true);
            AccessTools.Field(typeof(RelicDraftRewardData), "ignoreClassFilter").SetValue(data, configuration.GetSection("ignore_class_filter").ParseBool() ?? false);
            AccessTools.Field(typeof(RelicDraftRewardData), "draftOptionsCount").SetValue(data, configuration.GetSection("draft_options_count").ParseInt() ?? 3);
            var classType = configuration.GetSection("class_type").ParseClassType() ?? RunState.ClassType.None;
            AccessTools.Field(typeof(RelicDraftRewardData), "classType").SetValue(data, classType);
        }
    }
}
