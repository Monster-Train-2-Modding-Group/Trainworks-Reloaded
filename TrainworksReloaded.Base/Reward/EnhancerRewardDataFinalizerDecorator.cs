using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Reward
{
    public class EnhancerRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<EnhancerRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IDataFinalizer decoratee;

        public EnhancerRewardDataFinalizerDecorator(
            IModLogger<EnhancerRewardDataFinalizerDecorator> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<RelicData> relicRegister,
            IRegister<CardUpgradeData> upgradeRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.relicRegister = relicRegister;
            this.upgradeRegister = upgradeRegister;
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

            if (data1 is not EnhancerRewardData data)
                return;

            var configuration = configuration1.GetExtension("enhancer");

            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Enhancer Reward Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var relicReference = configuration.GetSection("enhancer").ParseReference();
            if (
                relicReference != null
                && relicRegister.TryLookupName(
                    relicReference.ToId(key, TemplateConstants.RelicData),
                    out var lookup,
                    out var _,
                    relicReference.context
                )
            )
            {
                if (lookup is not EnhancerData relic)
                {
                    logger.Log(LogLevel.Warning, $"Relic data name: {lookup?.name} given is not a EnhancerData ignoring.");
                }
                else
                {
                    AccessTools.Field(typeof(EnhancerRewardData), "_enhancerData").SetValue(data, relic);
                    AccessTools.Field(typeof(EnhancerRewardData), "_enhancerDataId").SetValue(data, relic.GetID());
                }
            }

            var upgradeReferences = configuration.GetSection("additional_upgrade_options").ParseReferences();
            if (!upgradeReferences.IsNullOrEmpty())
            {
                List<CardUpgradeData> upgrades = [];
                foreach (var reference in upgradeReferences)
                {
                    if (reference != null && upgradeRegister.TryLookupName(reference.ToId(key, TemplateConstants.Upgrade), out var item, out var _, reference.context))
                    {
                        upgrades.Add(item);
                    }
                }
                var pool = ScriptableObject.CreateInstance<CardUpgradePool>();
                AccessTools.Field(typeof(CardUpgradePool), "cardUpgradeDataList").SetValue(pool, upgrades);
                AccessTools.Field(typeof(EnhancerRewardData), "additionalUpgradeOptions").SetValue(data, pool);
            }

            AccessTools.Field(typeof(EnhancerRewardData), "_cacheSelectedCard").SetValue(data, configuration.GetSection("cache_selected_card").ParseBool() ?? false);
            AccessTools.Field(typeof(EnhancerRewardData), "_useCacheCard").SetValue(data, configuration.GetSection("use_cache_card").ParseBool() ?? false);
        }
    }
}
