using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Reward
{
    public class CardRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<CardRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IRegister<CardData> cardRegister;
        private readonly IDataFinalizer decoratee;

        public CardRewardDataFinalizerDecorator(
            IModLogger<CardRewardDataFinalizerDecorator> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<CardData> cardRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.cardRegister = cardRegister;
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

            if (data1 is not CardRewardData data)
                return;

            var configuration = configuration1.GetExtension("card");

            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Card Reward Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var cardReference = configuration.GetSection("card").ParseReference();
            if (
                cardReference != null
                && cardRegister.TryLookupName(
                    cardReference.ToId(key, TemplateConstants.Card),
                    out var cardData,
                    out var _,
                    cardReference.context
                )
            )
            {
                AccessTools.Field(typeof(CardRewardData), "_cardData").SetValue(data, cardData);
                AccessTools.Field(typeof(CardRewardData), "_cardDataId").SetValue(data, cardData.GetID());
            }

            AccessTools.Field(typeof(CardRewardData), "_numCopies").SetValue(data, configuration.GetSection("num_copies").ParseInt() ?? 1);
        }
    }
}
