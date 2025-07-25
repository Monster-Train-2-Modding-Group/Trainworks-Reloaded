using HarmonyLib;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Reward
{
    public class RewardDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<RewardDataFinalizer> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IRegister<Sprite> spriteRegister;

        public RewardDataFinalizer(
            IModLogger<RewardDataFinalizer> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<Sprite> spriteRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRewardData(definition);
            }
            cache.Clear();
        }

        /// <summary>
        /// Finalize Card Definitions
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeRewardData(IDefinition<RewardData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Debug,
                $"Finalizing Reward Data {definition.Id.ToId(key, TemplateConstants.RewardData)}..."
            );

            var sprite = configuration.GetSection("sprite").ParseReference();
            if (
                sprite != null
                && spriteRegister.TryLookupId(
                    sprite.ToId(key, TemplateConstants.Sprite),
                    out var spriteLookup,
                    out var _
                )
            )
            {
                AccessTools.Field(typeof(RewardData), "_rewardSprite").SetValue(data, spriteLookup);
            }
        }
    }
}
