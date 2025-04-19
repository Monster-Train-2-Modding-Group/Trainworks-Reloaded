using System.Linq;
using System.Xml.Linq;
using HarmonyLib;
using Malee;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Class;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Map;
using TrainworksReloaded.Base.Relic;
using TrainworksReloaded.Core;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using TrainworksReloaded.Base.Prefab;
using UnityEngine;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(AssetLoadingManager), "Start")]
    public class InitializationPatch
    {
        public static void Postfix(AssetLoadingData ____assetLoadingData)
        {
            var container = Railend.GetContainer();
            // var gameObjectRegister = container.GetInstance<GameObjectRegister>();
            // gameObjectRegister.hiddenRoot.transform.position = new Vector3(10000, 10000, 0);
            var register = container.GetInstance<CardDataRegister>();
            var logger = container.GetInstance<IModLogger<InitializationPatch>>();

            logger.Log(LogLevel.Info, "Starting TrainworksReloaded initialization...");

            //add data to the existing main pools
            var delegator = container.GetInstance<VanillaCardPoolDelegator>();
            logger.Log(LogLevel.Info, "Processing card pools...");
            foreach (
                var cardpool in ____assetLoadingData.CardPoolsAll.Union(
                    ____assetLoadingData.CardPoolsAlwaysLoad
                )
            )
            {
                if (cardpool != null && delegator.CardPoolToData.ContainsKey(cardpool.name))
                {
                    var cardsToAdd = delegator.CardPoolToData[cardpool.name];
                    var dataList =
                        (ReorderableArray<CardData>)
                            AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(cardpool);
                    foreach (var card in cardsToAdd)
                    {
                        dataList.Add(card);
                    }
                    logger.Log(LogLevel.Debug, $"Added {cardsToAdd.Count} cards to pool: {cardpool.name}");
                }
            }
            delegator.CardPoolToData.Clear(); //save memory
            //we add custom card pool so that the card data is loaded, even if it doesn't exist in any pool.
            ____assetLoadingData.CardPoolsAll.Add(register.CustomCardPool);
            logger.Log(LogLevel.Info, "Card pool processing complete");

            //add relic data to megapool
            var relicDelegator = container.GetInstance<VanillaRelicPoolDelegator>();
            if (relicDelegator.RelicPoolToData.ContainsKey("megapool"))
            {
                logger.Log(LogLevel.Info, "Processing relic pools...");
                var ftueBlessingPool = ____assetLoadingData.BalanceData.GetFtueBlessingPool();
                var dataList =
                    (ReorderableArray<CollectableRelicData>)
                        AccessTools
                            .Field(typeof(RelicPool), "relicDataList")
                            .GetValue(ftueBlessingPool);
                foreach (var relic in relicDelegator.RelicPoolToData["megapool"])
                {
                    dataList.Add(relic);
                }
                logger.Log(LogLevel.Debug, $"Added {relicDelegator.RelicPoolToData["megapool"].Count} relics to megapool");
            }
            relicDelegator.RelicPoolToData.Clear();
            logger.Log(LogLevel.Info, "Relic pool processing complete");

            var classRegister = container.GetInstance<ClassDataRegister>();
            var classDatas =
                (List<ClassData>)
                    AccessTools
                        .Field(typeof(BalanceData), "classDatas")
                        .GetValue(____assetLoadingData.BalanceData);
            classDatas.AddRange(classRegister.Values);
            logger.Log(LogLevel.Info, $"Added {classRegister.Values.Count} custom classes");

            //handle map data
            logger.Log(LogLevel.Info, "Processing map data...");
            var mapDelegator = container.GetInstance<MapNodeDelegator>();
            var runDataDictionary = new Dictionary<string, RunData>
            {
                { "primary", ____assetLoadingData.BalanceData.GetRunData(false, false) },
                { "first_time", ____assetLoadingData.BalanceData.GetRunData(true, false) },
                { "endless", ____assetLoadingData.BalanceData.GetRunData(false, true) },
            };

            foreach (var kvp in runDataDictionary)
            {
                var runDataKey = kvp.Key;
                var bucketLists =
                    (ReorderableArray<MapNodeBucketList>)
                        AccessTools
                            .Field(typeof(RunData), "mapNodeBucketLists")
                            .GetValue(kvp.Value);

                if (bucketLists == null)
                    continue;

                foreach (var bucketList in bucketLists)
                {
                    foreach (var bucket in bucketList.BucketList)
                    {
                        var bucketId = (string)
                            AccessTools
                                .Field(typeof(MapNodeBucketContainer), "id")
                                .GetValue(bucket);

                        if (
                            mapDelegator.MapBucketToData.TryGetValue(
                                new MapNodeKey(runDataKey, bucketId),
                                out var values
                            )
                        )
                        {
                            var bucketDataList =
                                (ReorderableArray<MapNodeBucketData>)
                                    AccessTools
                                        .Field(
                                            typeof(MapNodeBucketContainer),
                                            "mapNodeBucketContainerList"
                                        )
                                        .GetValue(bucket);

                            foreach (var bucketData in bucketDataList)
                            {
                                bucketData.MapNodes.AddRange(values);
                            }
                            logger.Log(LogLevel.Debug, $"Added {values.Count} map nodes to bucket: {bucketId} in {runDataKey}");
                        }
                    }
                }
            }
            mapDelegator.MapBucketToData.Clear();
            logger.Log(LogLevel.Info, "Map data processing complete");

            //Load localization at this time
            logger.Log(LogLevel.Info, "Loading localization data...");
            var localization = container.GetInstance<CustomLocalizationTermRegistry>();
            localization.LoadData();
            logger.Log(LogLevel.Info, "Localization data loaded");

            //Add replacement strings
            logger.Log(LogLevel.Info, "Loading replacement strings...");
            var replacementStringRegistry = container.GetInstance<ReplacementStringRegistry>();
            replacementStringRegistry.LoadData();
            logger.Log(LogLevel.Info, "Replacement strings loaded");

            //Run finalization steps to populate data that required all other data to be loaded first
            logger.Log(LogLevel.Info, "Running finalization steps...");
            var finalizer = container.GetInstance<Finalizer>();
            finalizer.FinalizeData();
            logger.Log(LogLevel.Info, "TrainworksReloaded initialization complete!");
        }
    }
}
