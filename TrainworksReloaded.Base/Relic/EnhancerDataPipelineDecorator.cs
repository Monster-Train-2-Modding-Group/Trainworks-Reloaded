using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerDataPipelineDecorator : IDataPipeline<IRegister<RelicData>, RelicData>
    {
        private readonly IModLogger<EnhancerDataPipelineDecorator> logger;
        private readonly IDataPipeline<IRegister<RelicData>, RelicData> decoratee;
        private readonly VanillaEnhancerPoolDelegator enhancerPoolDelegator;

        public EnhancerDataPipelineDecorator(
            IModLogger<EnhancerDataPipelineDecorator> logger,
            IDataPipeline<IRegister<RelicData>, RelicData> decoratee,
            VanillaEnhancerPoolDelegator enhancerPoolDelegator
        )
        {
            this.logger = logger;
            this.decoratee = decoratee;
            this.enhancerPoolDelegator = enhancerPoolDelegator;
        }

        public List<IDefinition<RelicData>> Run(IRegister<RelicData> register)
        {
            var definitions = decoratee.Run(register);
            foreach (var definition in definitions)
            {
                ProcessEnhancerData(definition);
            }
            return definitions;
        }

        private void ProcessEnhancerData(IDefinition<RelicData> definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            if (data is not EnhancerData enhancer)
                return;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("enhancer").Exists())
                .Select(xs => xs.GetSection("enhancer"))
                .FirstOrDefault();
            if (configuration == null)
                return;

            // Handle rarity
            var rarity = configuration.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common;
            AccessTools.Field(typeof(EnhancerData), "rarity").SetValue(enhancer, rarity);

            // Handle unlock level
            var unlockLevel = configuration.GetSection("unlock_level").ParseInt() ?? 0;
            AccessTools.Field(typeof(EnhancerData), "unlockLevel").SetValue(enhancer, unlockLevel);

            var numCardsToShowInUpgradeScreen = configuration.GetSection("num_cards_to_show_in_upgrade_screen").ParseInt() ?? -1;
            AccessTools.Field(typeof(EnhancerData), "numCardsToShowInUpgradeScreen").SetValue(enhancer, numCardsToShowInUpgradeScreen);

            // Handle pools
            // TODO move to the finalizer and directly add to the EnhancerPool
            foreach (var child in configuration.GetSection("pools").GetChildren())
            {
                var pool = child?.ParseString();
                if (pool == null) continue;

                if (!enhancerPoolDelegator.EnhancerPoolToData.ContainsKey(pool))
                {
                    enhancerPoolDelegator.EnhancerPoolToData[pool] = [];
                }
                enhancerPoolDelegator.EnhancerPoolToData[pool].Add(enhancer);

                logger.Log(LogLevel.Debug, $"Added enhancer {definition.Id.ToId(key, TemplateConstants.RelicData)} to pool: {pool}");
            }
        }
    }
}