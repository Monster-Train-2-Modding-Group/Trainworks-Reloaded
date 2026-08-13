using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Reward
{
    public class PurgeRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<PurgeRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IRegister<CardUpgradeMaskData> filterRegister;
        private readonly IDataFinalizer decoratee;

        public PurgeRewardDataFinalizerDecorator(
            IModLogger<PurgeRewardDataFinalizerDecorator> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<CardUpgradeMaskData> filterRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.filterRegister = filterRegister;
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

            if (data1 is not PurgeRewardData data)
                return;

            var configuration = configuration1.GetExtension("purge");

            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Purge Reward Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var filterReference = configuration.GetSection("filter").ParseReference();
            if (
                filterReference != null
                && filterRegister.TryLookupName(
                    filterReference.ToId(key, TemplateConstants.UpgradeMask),
                    out var lookup,
                    out var _,
                    filterReference.context
                )
            )
            {
                AccessTools.Field(typeof(PurgeRewardData), "cardUpgradeMaskData").SetValue(data, lookup);
            }

            AccessTools.Field(typeof(PurgeRewardData), "isCompulsory").SetValue(data, configuration.GetSection("is_compulsory").ParseBool() ?? false);
            AccessTools.Field(typeof(PurgeRewardData), "numPurges").SetValue(data, configuration.GetSection("num_purges").ParseInt() ?? 1);
            AccessTools.Field(typeof(PurgeRewardData), "purgeRandomCard").SetValue(data, configuration.GetSection("purge_random_card").ParseBool() ?? false);
            AccessTools.Field(typeof(PurgeRewardData), "allowPurgeChampion").SetValue(data, configuration.GetSection("allow_purge_champion").ParseBool() ?? false);

            var costs = configuration
                .GetSection("costs")
                .GetChildren()
                .Select(xs => xs.ParseInt() ?? 0)
                .ToList();
            AccessTools.Field(typeof(PurgeRewardData), "secondaryCosts").SetValue(data, costs.ToArray());
        }
    }
}
