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
                ProcessCollectableRelicData(definition);
            }
            return definitions;
        }

        private void ProcessCollectableRelicData(IDefinition<RelicData> definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            if (data is not CollectableRelicData collectableRelic)
                return;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("collectable").Exists())
                .Select(xs => xs.GetSection("collectable"))
                .FirstOrDefault();
            if (configuration == null)
                return;

            // Handle rarity
            var rarity = configuration.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common;
            AccessTools.Field(typeof(CollectableRelicData), "rarity").SetValue(collectableRelic, rarity);

            // Handle unlock level
            var unlockLevel = configuration.GetSection("unlock_level").ParseInt() ?? 0;
            AccessTools.Field(typeof(CollectableRelicData), "unlockLevel").SetValue(collectableRelic, unlockLevel);

            // Handle story event flag
            var fromStoryEvent = configuration.GetSection("from_story_event").ParseBool() ?? false;
            AccessTools.Field(typeof(CollectableRelicData), "fromStoryEvent").SetValue(collectableRelic, fromStoryEvent);

            // Handle boss given flag
            var isBossGivenRelic = configuration.GetSection("is_boss_given").ParseBool() ?? false;
            AccessTools.Field(typeof(CollectableRelicData), "isBossGivenRelic").SetValue(collectableRelic, isBossGivenRelic);

            // Handle dragon's hoard flag
            var isDragonsHoardRelic = configuration.GetSection("is_dragons_hoard").ParseBool() ?? false;
            AccessTools.Field(typeof(CollectableRelicData), "isDragonsHoardRelic").SetValue(collectableRelic, isDragonsHoardRelic);

            // Handle ignore for no relic achievement flag
            var ignoreForNoRelicAchievement = configuration.GetSection("ignore_for_no_relic_achievement").ParseBool() ?? false;
            AccessTools.Field(typeof(CollectableRelicData), "ignoreForNoRelicAchievement").SetValue(collectableRelic, ignoreForNoRelicAchievement);

            // Handle required DLC
            var requiredDLC = configuration.GetSection("required_dlc").ParseDLC() ?? ShinyShoe.DLC.None;
            AccessTools.Field(typeof(CollectableRelicData), "requiredDLC").SetValue(collectableRelic, requiredDLC);

            // Handle FTUE deprioritization flag
            var deprioritizeInFtueDrafts = configuration.GetSection("deprioritize_in_ftue_drafts").ParseBool() ?? false;
            AccessTools.Field(typeof(CollectableRelicData), "deprioritizeInFtueDrafts").SetValue(collectableRelic, deprioritizeInFtueDrafts);

            // Handle force update count label flag
            var forceUpdateCountLabel = configuration.GetSection("force_update_count_label").ParseBool() ?? false;
            AccessTools.Field(typeof(CollectableRelicData), "forceUpdateCountLabel").SetValue(collectableRelic, forceUpdateCountLabel);
            // Handle pool
            // TODO remove in favor of pools.
            var pool = configuration.GetSection("pool").ParseString();
            if (pool != null)
            {
                logger.Log(LogLevel.Warning, "[Deprecation] relics.pool is deprecated and will be removed soon use relics.pools instead.");
                if (!relicPoolDelegator.RelicPoolToData.ContainsKey(pool))
                {
                    relicPoolDelegator.RelicPoolToData[pool] = [];
                }
                relicPoolDelegator.RelicPoolToData[pool].Add(collectableRelic);
                logger.Log(LogLevel.Debug, $"Added relic {definition.Id.ToId(key, TemplateConstants.RelicData)} to pool: {pool}");
            }

            // Handle pools
            // TODO move to the finalizer and directly add to the RelicPool
            foreach (var child in configuration.GetSection("pools").GetChildren())
            {
                var relicPool = child?.ParseString();
                if (relicPool == null) continue;

                if (!relicPoolDelegator.RelicPoolToData.ContainsKey(relicPool))
                {
                    relicPoolDelegator.RelicPoolToData[relicPool] = [];
                }
                relicPoolDelegator.RelicPoolToData[relicPool].Add(collectableRelic);

                logger.Log(LogLevel.Debug, $"Added relic {definition.Id.ToId(key, TemplateConstants.RelicData)} to pool: {relicPool}");
            }
        }
    }
}