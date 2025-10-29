using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Map
{
    public class RewardNodeDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<RewardNodeDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<MapNodeData>> cache;
        private readonly IRegister<ClassData> classDataRegister;
        private readonly IRegister<RewardData> rewardDataRegister;
        private readonly IDataFinalizer decoratee;

        public RewardNodeDataFinalizerDecorator(
            IModLogger<RewardNodeDataFinalizerDecorator> logger,
            ICache<IDefinition<MapNodeData>> cache,
            IRegister<ClassData> classDataRegister,
            IRegister<RewardData> rewardDataRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.classDataRegister = classDataRegister;
            this.rewardDataRegister = rewardDataRegister;
            this.decoratee = decoratee;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRewardNodeData(definition);
            }
            decoratee.FinalizeData();
            cache.Clear();
        }

        /// <summary>
        /// Finalize RewardNode Definitions
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeRewardNodeData(IDefinition<MapNodeData> definition)
        {
            var configuration1 = definition.Configuration;
            var data1 = definition.Data;
            var key = definition.Key;
            if (data1 is not RewardNodeData data)
                return;

            var configuration = configuration1
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("reward").Exists())
                .Select(xs => xs.GetSection("reward"))
                .First();
            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Reward Node Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            //class
            var required_class = configuration.GetDeprecatedSection("required_class", "class").ParseReference();
            if (
                required_class != null
                && classDataRegister.TryLookupName(
                    required_class.ToId(key, TemplateConstants.Class),
                    out var classData,
                    out var _,
                    required_class.context
                )
            )
            {
                AccessTools
                    .Field(typeof(RewardNodeData), "requiredClass")
                    .SetValue(data, classData);
            }

            //rewards
            var rewards = new List<RewardData>();
            var rewardsReferences = configuration.GetSection("rewards")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in rewardsReferences)
            {
                if (rewardDataRegister.TryLookupId(
                        reference.ToId(key, TemplateConstants.RewardData),
                        out var rewardData,
                        out var _,
                        reference.context
                    )
                )
                {
                    rewards.Add(rewardData);
                }
            }

            AccessTools.Field(typeof(RewardNodeData), "rewards").SetValue(data, rewards);
        }
    }
}
