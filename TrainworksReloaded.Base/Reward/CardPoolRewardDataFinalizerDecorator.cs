using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Reward
{
    public class CardPoolRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<CardPoolRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IRegister<CardPool> cardPoolRegister;
        private readonly IDataFinalizer decoratee;

        public CardPoolRewardDataFinalizerDecorator(
            IModLogger<CardPoolRewardDataFinalizerDecorator> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<CardPool> cardPoolRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.cardPoolRegister = cardPoolRegister;
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
        /// Finalize Card Definitions
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeRewardData(IDefinition<RewardData> definition)
        {
            var configuration1 = definition.Configuration;
            var data1 = definition.Data;
            var key = definition.Key;
            if (data1 is not CardPoolRewardData data)
                return;

            var configuration = configuration1
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("cardpool").Exists())
                .Select(xs => xs.GetSection("cardpool"))
                .First() as IConfiguration;
            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Card Pool Reward Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            //cardpool
            var cardpool = configuration.GetDeprecatedSection("cardpool", "card_pool").ParseReference();
            if (
                cardpool != null
                && cardPoolRegister.TryLookupId(
                    cardpool.ToId(key, TemplateConstants.CardPool),
                    out var cardpoolData,
                    out var _,
                    cardpool.context
                )
            )
            {
                AccessTools
                    .Field(typeof(CardPoolRewardData), "cardPool")
                    .SetValue(data, cardpoolData);
            }

            //card costs
            var cardCosts = configuration
                .GetSection("cost_overrides")
                .GetChildren()
                .Select(xs => new CardPoolRewardData.CardCosts()
                {
                    rarity = xs.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common,
                    costs =
                    [
                        .. xs.GetSection("costs").GetChildren().Select(xs => xs.ParseInt() ?? 0),
                    ],
                })
                .ToList();
            AccessTools
                .Field(typeof(CardPoolRewardData), "cardCostsOverride")
                .SetValue(data, cardCosts);
        }
    }
}
