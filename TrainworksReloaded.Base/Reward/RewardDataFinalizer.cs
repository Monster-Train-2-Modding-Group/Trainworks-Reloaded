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
        private readonly IRegister<RelicData> relicRegister;

        public RewardDataFinalizer(
            IModLogger<RewardDataFinalizer> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<Sprite> spriteRegister,
            IRegister<RelicData> relicRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
            this.relicRegister = relicRegister;
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

            logger.Log(LogLevel.Info,
                $"Finalizing Reward Data {definition.Key} {definition.Id} path: {configuration.GetPath()}..."
            );

            var sprite = configuration.GetSection("sprite").ParseReference();
            if (
                sprite != null
                && spriteRegister.TryLookupId(
                    sprite.ToId(key, TemplateConstants.Sprite),
                    out var spriteLookup,
                    out var _,
                    sprite.context
                )
            )
            {
                AccessTools.Field(typeof(RewardData), "_rewardSprite").SetValue(data, spriteLookup);
            }

            var mutatorReference = configuration.GetSection("requires_mutator").ParseReference();
            if (mutatorReference != null)
            {
                relicRegister.TryLookupName(
                    mutatorReference.ToId(key, TemplateConstants.RelicData),
                    out var relic,
                    out var _,
                    mutatorReference.context);
                if (relic is not MutatorData)
                {
                    logger.Log(LogLevel.Error, $"{configuration.GetPath()} - requires_mutator requires a MutatorData got class {relic?.GetType()} instead. Ignoring.");
                }
                else
                {
                    AccessTools.Field(typeof(RewardData), "_requiresMutator").SetValue(data, relic);
                }

            }
        }
    }
}
