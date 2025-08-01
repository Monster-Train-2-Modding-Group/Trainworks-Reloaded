using HarmonyLib;
using Malee;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Class;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Map;
using TrainworksReloaded.Base.Relic;
using TrainworksReloaded.Base.Scenarios;
using TrainworksReloaded.Core;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(AssetLoadingManager), "Start")]
    public class InitializationPatch
    {
        public static void Postfix(AssetLoadingData ____assetLoadingData)
        {
            var container = Railend.GetContainer();
            RunSetupPatches.container = container;
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
            logger.Log(LogLevel.Info, "Processing relic pools...");
            foreach (var poolName in relicDelegator.RelicPoolToData.Keys)
            {
                var relicPool = RelicPoolRegister.GetVanillaRelicPool(____assetLoadingData.AllGameData, poolName);
                if (relicPool == null)
                {
                    logger.Log(LogLevel.Warning, $"Could not find relic pool associated with {poolName}!");
                    continue;
                }
                var dataList =
                    (ReorderableArray<CollectableRelicData>)
                        AccessTools
                            .Field(typeof(RelicPool), "relicDataList")
                            .GetValue(relicPool);
                foreach (var relic in relicDelegator.RelicPoolToData[poolName])
                {
                    dataList.Add(relic);
                }
                logger.Log(LogLevel.Debug, $"Added {relicDelegator.RelicPoolToData[poolName].Count} relics to {poolName}");
            }
            relicDelegator.RelicPoolToData.Clear();
            logger.Log(LogLevel.Info, "Relic pool processing complete");

            logger.Log(LogLevel.Info, "Processing enhancer pools...");
            var enhancerDelegator = container.GetInstance<VanillaEnhancerPoolDelegator>();
            foreach (var poolName in enhancerDelegator.EnhancerPoolToData.Keys)
            {
                var enhancerPool = EnhancerPoolRegister.GetVanillaEnhancerPool(____assetLoadingData.AllGameData, poolName);
                if (enhancerPool == null)
                {
                    logger.Log(LogLevel.Warning, $"Could not find enhancer pool associated with {poolName}!");
                    continue;
                }
                var dataList =
                    (ReorderableArray<EnhancerData>)
                        AccessTools
                            .Field(typeof(EnhancerPool), "relicDataList")
                            .GetValue(enhancerPool);
                foreach (var enhancer in enhancerDelegator.EnhancerPoolToData[poolName])
                {
                    dataList.Add(enhancer);
                }
                logger.Log(LogLevel.Debug, $"Added {enhancerDelegator.EnhancerPoolToData[poolName].Count} enhancers to {poolName}");
            }
            logger.Log(LogLevel.Info, "Enhancer pool processing complete");

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

            var scenarioDelegator = container.GetInstance<ScenarioDelegator>();
            foreach (var scenarioEntry in scenarioDelegator.Scenarios)
            {
                var distance = scenarioEntry.Distance;
                var runType = scenarioEntry.RunType;
                var scenario = scenarioEntry.Scenario;

                var runData = runDataDictionary[runType];
                if (runData == null)
                {
                    logger.Log(LogLevel.Warning, $"For scenario {scenario.name} could not find run type: {runType} valid runTypes are first_time, primary, and endless ignoring...");
                    continue;
                }

                var distanceData = runData.GetDistanceData(distance);
                if (distanceData == null)
                {
                    logger.Log(LogLevel.Warning, $"For scenario {scenario.name} could not find NodeDistanceData for distance: {distance} in run_type: {runType} ignoring...");
                    continue;
                }

                var distanceScenarios = (List<ScenarioData>)AccessTools.Field(typeof(NodeDistanceData), "battleDatas").GetValue(distanceData);
                distanceScenarios.Add(scenario);
                logger.Log(LogLevel.Info, $"Added scenario {scenario.name} to {distance} and run_type {runType} successfully.");
            }
            logger.Log(LogLevel.Info, "Map data processing complete");

            //Run finalization steps to populate data that requires all other data to be loaded first
            logger.Log(LogLevel.Info, "Running finalization steps...");
            var finalizer = container.GetInstance<Finalizer>();
            finalizer.FinalizeData();

            //Load localization at this time
            logger.Log(LogLevel.Info, "Loading localization data...");
            var languageSource = container.GetInstance<LanguageSourceRegister>();
            languageSource.LoadData();
            var localization = container.GetInstance<CustomLocalizationTermRegistry>();
            localization.LoadData(languageSource.Keys.ToList());
            logger.Log(LogLevel.Info, "Localization data loaded");

            //Add replacement strings
            logger.Log(LogLevel.Info, "Loading replacement strings...");
            var replacementStringRegistry = container.GetInstance<ReplacementStringRegistry>();
            replacementStringRegistry.LoadData();
            logger.Log(LogLevel.Info, "Replacement strings loaded");

            logger.Log(LogLevel.Info, "TrainworksReloaded initialization complete!");
        }

    }
}
