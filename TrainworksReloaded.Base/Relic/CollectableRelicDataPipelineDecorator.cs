using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class CollectableRelicDataPipelineDecorator : IDataPipeline<IRegister<RelicData>, RelicData>
    {
        private readonly IModLogger<CollectableRelicDataPipelineDecorator> logger;
        private readonly IDataPipeline<IRegister<RelicData>, RelicData> decoratee;
        private readonly VanillaRelicPoolDelegator relicPoolDelegator;

        public CollectableRelicDataPipelineDecorator(
            IModLogger<CollectableRelicDataPipelineDecorator> logger,
            IDataPipeline<IRegister<RelicData>, RelicData> decoratee,
            VanillaRelicPoolDelegator relicPoolDelegator
        )
        {
            this.logger = logger;
            this.decoratee = decoratee;
            this.relicPoolDelegator = relicPoolDelegator;
        }

        public List<IDefinition<RelicData>> Run(IRegister<RelicData> register)
        {
            var definitions = decoratee.Run(register);
            foreach (var definition in definitions)
            {
                ProcessCollectableRelicData((definition as RelicDataDefinition)!);
            }
            return definitions;
        }

        private void ProcessCollectableRelicData(RelicDataDefinition definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var overrideMode = definition.Override;
            var key = definition.Key;
            var relicId = definition.Id.ToId(key, TemplateConstants.RelicData);

            if (data is not CollectableRelicData relic)
                return;

            if (definition.CopyData is not CollectableRelicData copyData)
                copyData = relic;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("collectable").Exists())
                .Select(xs => xs.GetSection("collectable"))
                .FirstOrDefault();
            if (configuration == null)
                return;

            // Handle rarity
            var rarity = configuration.GetSection("rarity").ParseRarity() ?? copyData.GetRarity();
            AccessTools.Field(typeof(CollectableRelicData), "rarity").SetValue(relic, rarity);

            // Handle unlock level
            var unlockLevel = configuration.GetSection("unlock_level").ParseInt() ?? copyData.GetUnlockLevel();
            AccessTools.Field(typeof(CollectableRelicData), "unlockLevel").SetValue(relic, unlockLevel);

            // Handle story event flag
            var fromStoryEvent = configuration.GetSection("from_story_event").ParseBool() ?? copyData.GetFromStoryEvent();
            AccessTools.Field(typeof(CollectableRelicData), "fromStoryEvent").SetValue(relic, fromStoryEvent);

            // Handle boss given flag
            var isBossGivenRelic = configuration.GetSection("is_boss_given").ParseBool() ?? copyData.IsBossGivenRelic();
            AccessTools.Field(typeof(CollectableRelicData), "isBossGivenRelic").SetValue(relic, isBossGivenRelic);

            // Handle dragon's hoard flag
            var isDragonsHoardRelic = configuration.GetSection("is_dragons_hoard").ParseBool() ?? copyData.IsDragonsHoardRelic();
            AccessTools.Field(typeof(CollectableRelicData), "isDragonsHoardRelic").SetValue(relic, isDragonsHoardRelic);

            // Handle ignore for no relic achievement flag
            var ignoreForNoRelicAchievement = configuration.GetSection("ignore_for_no_relic_achievement").ParseBool() ?? copyData.IgnoreForNoRelicAchievement();
            AccessTools.Field(typeof(CollectableRelicData), "ignoreForNoRelicAchievement").SetValue(relic, ignoreForNoRelicAchievement);

            // Handle required DLC
            var requiredDLC = configuration.GetSection("required_dlc").ParseDLC() ?? copyData.GetRequiredDLC();
            AccessTools.Field(typeof(CollectableRelicData), "requiredDLC").SetValue(relic, requiredDLC);

            // Handle FTUE deprioritization flag
            var deprioritizeInFtueDrafts = configuration.GetSection("deprioritize_in_ftue_drafts").ParseBool() ?? copyData.GetIsDeprioritizedInFtueDrafts();
            AccessTools.Field(typeof(CollectableRelicData), "deprioritizeInFtueDrafts").SetValue(relic, deprioritizeInFtueDrafts);

            // Handle force update count label flag
            var forceUpdateCountLabel = configuration.GetSection("force_update_count_label").ParseBool() ?? copyData.ShouldForceUpdateCountLabel();
            AccessTools.Field(typeof(CollectableRelicData), "forceUpdateCountLabel").SetValue(relic, forceUpdateCountLabel);
        }
    }
}