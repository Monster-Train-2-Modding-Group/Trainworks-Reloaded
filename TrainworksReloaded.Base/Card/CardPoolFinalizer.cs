using HarmonyLib;
using Malee;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Card
{
    public class CardPoolFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CardPoolFinalizer> logger;
        private readonly ICache<IDefinition<CardPool>> cache;
        private readonly IRegister<CardData> cardRegister;

        public CardPoolFinalizer(
            IModLogger<CardPoolFinalizer> logger,
            ICache<IDefinition<CardPool>> cache,
            IRegister<CardData> cardRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.cardRegister = cardRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCardPoolData(definition);
            }
            cache.Clear();
        }

        private void FinalizeCardPoolData(IDefinition<CardPool> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing Card Pool {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            //handle cards
            var cardDatas = new List<CardData>();
            var cardReferences = configuration.GetSection("cards")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in cardReferences)
            {
                var id = reference.ToId(key, TemplateConstants.Card);
                if (cardRegister.TryLookupName(id, out var card, out var _, reference.context))
                {
                    cardDatas.Add(card);
                }
            }
            if (cardDatas.Count != 0)
            {
                var cardDataList =
                    (ReorderableArray<CardData>)
                        AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(data);
                cardDataList.Clear();
                foreach (var item in cardDatas)
                {
                    cardDataList.Add(item);
                }
                AccessTools.Field(typeof(CardPool), "cardDataList").SetValue(data, cardDataList);
            }
        }
    }
}
