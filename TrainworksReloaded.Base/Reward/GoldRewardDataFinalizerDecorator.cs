using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Reward
{
    public class GoldRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<GoldRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IDataFinalizer decoratee;

        public GoldRewardDataFinalizerDecorator(
            IModLogger<GoldRewardDataFinalizerDecorator> logger,
            ICache<IDefinition<RewardData>> cache,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
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

            if (data1 is not GoldRewardData data)
                return;

            var configuration = configuration1.GetExtension("gold");

            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Gold Reward Data {key} {definition.Id} path: {configuration.GetPath()}...");

            var amount = configuration.GetSection("amount").ParseInt() ?? 0;
            AccessTools.Field(typeof(GoldRewardData), "_amount").SetValue(data, amount);
        }
    }
}
