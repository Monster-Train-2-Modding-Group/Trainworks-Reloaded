using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerPoolRegister : Dictionary<string, EnhancerPool>, IRegister<EnhancerPool>
    {
        private readonly IModLogger<EnhancerPoolRegister> logger;
        private readonly Lazy<SaveManager> SaveManager;

        public EnhancerPoolRegister(GameDataClient client, IModLogger<EnhancerPoolRegister> logger)
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
            this.logger = logger;
        }

        public void Register(string key, EnhancerPool item)
        {
            logger.Log(LogLevel.Info, $"Register Enhancer Pool {key}...");
            Add(key, item);
        }


        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out EnhancerPool? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {

            if (this.TryGetValue(identifier, out lookup))
            {
                IsModded = true;
                return true;
            }
            else
            {
                lookup = GetVanillaEnhancerPool(SaveManager.Value.GetAllGameData(), identifier);
                IsModded = false;
                return lookup != null;
            }
        }

        public static EnhancerPool? GetVanillaEnhancerPool(AllGameData allGameData, string poolName)
        {
            if (poolName == "MalickaDraftUpgradePool")
            {
                PyreArtifactData? malickaPyre = allGameData.FindPyreArtifactData("68a9b977-3407-4128-bf35-245fd92f8e2b");
                var effect = malickaPyre?.GetFirstRelicEffectData<RelicEffectAddStartingUpgradeToCardDrafts>();
                return effect?.GetParamEnhancerPool();
            }
            else if (poolName == "DraftUpgradePool")
            {
                CollectableRelicData? capriciousReflection = allGameData.FindCollectableRelicData("9e0e5d4e-6d16-43f1-8cd4-cc4c2b431afd");
                var effect = capriciousReflection?.GetFirstRelicEffectData<RelicEffectAddStartingUpgradeToCardDrafts>();
                return effect?.GetParamEnhancerPool();
            }
            else if (poolName != null)
            {
                IReadOnlyList<string> Merchants = [
                    "ed1b1cfa-9da2-4588-85fe-6360913ef41e", // Unit Upgrades
                    "9a70610f-8900-4900-b96d-4f88faa0f105", // Spell Upgrades
                    "f57bc1e9-4e86-4abf-af2a-0d56d2b3f59a", // Artifact Merchant
                    "e2c67b52-4d52-48b5-b20a-c6f4c12e44fa"  // Equipment Merchant
                ];
                foreach (var merchantID in Merchants)
                {
                    var mapNode = allGameData.FindMapNodeData(merchantID);
                    if (mapNode is MerchantData merchant)
                    {
                        for (int i = 0; i < merchant.GetNumRewards(); i++)
                        {
                            RewardData reward = merchant.GetReward(i).RewardData;
                            if (reward is EnhancerPoolRewardData enhancerPoolReward)
                            {
                                var foundEnhancerPool = (EnhancerPool)AccessTools.Field(typeof(EnhancerPoolRewardData), "relicPool").GetValue(enhancerPoolReward);
                                if (foundEnhancerPool.name == poolName)
                                {
                                    return foundEnhancerPool;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
