using HarmonyLib;
using System;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class CardTriggerTypeFinalizer : IDataFinalizer
    {
        private readonly Lazy<SaveManager> SaveManager;
        private readonly GameDataClient client;
        private readonly IModLogger<CardTriggerTypeFinalizer> logger;
        private readonly ICache<IDefinition<CardTriggerType>> cache;

        public CardTriggerTypeFinalizer(
            IModLogger<CardTriggerTypeFinalizer> logger,
            GameDataClient client,
            ICache<IDefinition<CardTriggerType>> cache
        )
        {
            SaveManager = new Lazy<SaveManager>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    return provider;
                }
                else
                {
                    return new SaveManager();
                }
            });
            this.client = client;
            this.logger = logger;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeTrigger(definition);
            }
            cache.Clear();
        }

        private void FinalizeTrigger(IDefinition<CardTriggerType> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var trigger = definition.Data;
            var id = definition.Id;

            logger.Log(LogLevel.Info, $"Finalizing Card Trigger {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var baseKey = "CardTrigger_" + id;

            var triggerLocalizationDict = (Dictionary<CardTriggerType, string>)AccessTools
                .Field(typeof(CardTriggerTypeMethods), "TriggerToLocalizationExpression").GetValue(null);
            triggerLocalizationDict[trigger] = baseKey;

            var disableInDeployment = configuration.GetSection("disallow_in_deployment").ParseBool() ?? false;
            if (disableInDeployment)
            {
                var balanceData = SaveManager.Value.GetAllGameData().GetBalanceData();
                var cardTriggers = (List<CardTriggerType>)AccessTools
                    .Field(typeof(BalanceData), "disallowedDeploymentPhaseCardTriggers").GetValue(balanceData);
                cardTriggers.Add(trigger);
            }

            // TODO CardTrigger => CharacterTrigger association.
        }
    }
}
