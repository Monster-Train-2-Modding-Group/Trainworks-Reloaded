using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static DraftRewardData;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Map
{
    public class RewardNodeDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<RewardNodeDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<MapNodeData>> cache;
        private readonly IRegister<ClassData> classDataRegister;
        private readonly IRegister<RewardData> rewardDataRegister;
        private readonly CardPoolRegister cardPoolRegister;
        private readonly IDataFinalizer decoratee;
        private readonly Lazy<SaveManager> SaveManager;

        private readonly FieldInfo RelicDraftPoolSubstitutionsField = AccessTools.Field(typeof(DraftRewardData), "relicDraftPoolSubstitutions");
        private readonly FieldInfo ReplacementDraftPoolField = AccessTools.Field(typeof(DraftRewardData.RelicDraftPoolSubstitution), "replacementDraftPool");
        private readonly FieldInfo RelicDataField = AccessTools.Field(typeof(DraftRewardData.RelicDraftPoolSubstitution), "relicData");

        private const string BANNERED_MUTATOR_ASSET_NAME = "Echoes_ReplaceBannerRewardsWithBannerSpellRewards";
        private const string UNIT_BANNER_REWARD_ASSET_NAME = "CardDraftLevelUpUnitUmbra";

        public RewardNodeDataFinalizerDecorator(
            IModLogger<RewardNodeDataFinalizerDecorator> logger,
            ICache<IDefinition<MapNodeData>> cache,
            IRegister<ClassData> classDataRegister,
            IRegister<RewardData> rewardDataRegister,
            CardPoolRegister cardPoolRegister,
            GameDataClient client,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.classDataRegister = classDataRegister;
            this.rewardDataRegister = rewardDataRegister;
            this.cardPoolRegister = cardPoolRegister;
            this.decoratee = decoratee;
            SaveManager = new Lazy<SaveManager>(() =>
            {
                if (client.TryGetValue(typeof(SaveManager), out var details))
                {
                    return (SaveManager)details.Provider;
                }
                else
                {
                    return new SaveManager();
                }
            });
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
            ClassData? classData = null;
            if (
                required_class != null
                && classDataRegister.TryLookupName(
                    required_class.ToId(key, TemplateConstants.Class),
                    out classData,
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
                    if (rewardData is DraftRewardData draftRewardData && data.GetIsBannerNode())
                    {
                        AddRelicDraftSubstitutions(draftRewardData, classData);
                    }
                }
            }

            AccessTools.Field(typeof(RewardNodeData), "rewards").SetValue(data, rewards);
        }

        private void AddRelicDraftSubstitutions(DraftRewardData draftRewardData, ClassData? classData)
        {
            if (classData == null) 
                return;

            var drd = SaveManager.Value.GetAllGameData().FindRewardDataByName(UNIT_BANNER_REWARD_ASSET_NAME) as DraftRewardData;
            var existing = RelicDraftPoolSubstitutionsField.GetValue(drd) as List<DraftRewardData.RelicDraftPoolSubstitution>;
            if (existing != null)
            {
                var substitutions = new List<DraftRewardData.RelicDraftPoolSubstitution>(existing);
                for (int i = 0; i < substitutions.Count; i++)
                {
                    var sub = substitutions[i];
                    if (sub.RelicData.name == BANNERED_MUTATOR_ASSET_NAME)
                    {
                        var newItem = new DraftRewardData.RelicDraftPoolSubstitution();
                        var replacementDraftPool = cardPoolRegister.GetBannerReplacementPool(classData.name);
                        RelicDataField.SetValue(newItem, sub.RelicData);
                        ReplacementDraftPoolField.SetValue(newItem, replacementDraftPool);
                        substitutions[i] = newItem;
                    }
                }
                RelicDraftPoolSubstitutionsField.SetValue(draftRewardData, substitutions);
            }
        }
    }
}
