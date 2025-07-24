using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Reward
{
    public class GrantableRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<GrantableRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IDataFinalizer decoratee;

        public GrantableRewardDataFinalizerDecorator(
            IModLogger<GrantableRewardDataFinalizerDecorator> logger,
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

        /// <summary>
        /// Finalize Grantable Reward Definitions
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeRewardData(IDefinition<RewardData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            if (data is not GrantableRewardData draftData)
                return;

            var draftConfiguration = configuration
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("grantable").Exists())
                .Select(xs => xs.GetSection("grantable"))
                .FirstOrDefault();
            if (draftConfiguration == null)
                return;

            logger.Log(LogLevel.Debug,
                $"Finalizing Grantable Reward Data {definition.Id.ToId(key, TemplateConstants.RewardData)}..."
            );

            // Set GrantableRewardData fields
            var isServiceMerchantReward = draftConfiguration.GetSection("is_service_merchant_reward").ParseBool() ?? false;
            AccessTools.Field(typeof(GrantableRewardData), "_isServiceMerchantReward").SetValue(draftData, isServiceMerchantReward);

            var merchantServiceIndex = draftConfiguration.GetSection("merchant_service_index").ParseInt() ?? 0;
            AccessTools.Field(typeof(GrantableRewardData), "_merchantServiceIndex").SetValue(draftData, merchantServiceIndex);

            var applyTrialDataModifiers = draftConfiguration.GetSection("apply_trial_data_modifiers").ParseBool() ?? false;
            AccessTools.Field(typeof(GrantableRewardData), "_applyTrialDataModifiers").SetValue(draftData, applyTrialDataModifiers);
        }
    }
}