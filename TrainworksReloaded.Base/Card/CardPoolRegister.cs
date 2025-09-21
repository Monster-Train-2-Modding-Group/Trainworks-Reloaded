using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Card
{
    public class CardPoolRegister : Dictionary<string, CardPool>, IRegister<CardPool>
    {
        private readonly IModLogger<CardPoolRegister> logger;
        private readonly IDictionary<string, CardPool> VanillaCardPools = new Dictionary<string, CardPool>();

        public CardPoolRegister(GameDataClient client, IModLogger<CardPoolRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, CardPool item)
        {
            logger.Log(LogLevel.Info, $"Register Card Pool {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out CardPool? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            IsModded = false;
            if (this.TryGetValue(identifier, out lookup))
            {
                IsModded = true;
                return true;
            }
            lookup = GetVanillaRelicPool(identifier);
            return lookup != null;
        }

        public CardPool? GetVanillaRelicPool(string poolName)
        {
            if (VanillaCardPools.TryGetValue(poolName, out CardPool ret))
            {
                return ret;
            }
            if (!ContainsKey(poolName))
            {
                logger.Log(LogLevel.Warning, $"Vanilla CardPool {poolName} is not supported as no fetcher for this pool exists yet.");
            }
            return null;
        }

        // Called as part of the Initailization Patch.
        public void PreloadAllVanillaCardPools(AllGameData allGameData)
        {
            if (VanillaCardPools.Count != 0)
            {
                logger.Log(LogLevel.Warning, "Card Pools have already been loaded, ignoring this call.");
                return;
            }

            var allowedCardPoolsField = AccessTools.Field(typeof(CardUpgradeMaskData), "allowedCardPools");
            var disallowedCardPoolsField = AccessTools.Field(typeof(CardUpgradeMaskData), "disallowedCardPools");
            var cardPoolRewardDataField = AccessTools.Field(typeof(CardPoolRewardData), "cardPool");
            var equipmentGraftRewardDataField = AccessTools.Field(typeof(EquipmentRandomGraftRewardData), "equipmentCardPool");

            var merchant = allGameData.FindMapNodeData(/*Equipment Merchant*/ "e2c67b52-4d52-48b5-b20a-c6f4c12e44fa") as MerchantData;
            var swordmaiden = allGameData.FindCharacterData(/*Swordmaiden*/ "f58d2cb6-0c2c-42f5-98e2-69d4354d42c6") as CharacterData;

            RewardData? cardPoolReward;
            CardUpgradeMaskData? filter;

            void AddVanillaCardPool(CardPool? cardPool)
            {
                if (cardPool == null) 
                    return;
                logger.Log(LogLevel.Debug, $"Found Vanilla CardPool {cardPool.name}");
                VanillaCardPools.Add(cardPool.name, cardPool);
            }

            void AddVanillaCardPoolsFromFilter(CardUpgradeMaskData? mask)
            {
                if (mask == null) return;
                var pools = allowedCardPoolsField.GetValue(mask) as List<CardPool>;
                if (pools != null)
                {
                    foreach (var pool in pools)
                    {
                        AddVanillaCardPool(pool);
                    }
                }
                pools = disallowedCardPoolsField.GetValue(mask) as List<CardPool>;
                if (pools != null)
                {
                    foreach (var pool in pools)
                    {
                        AddVanillaCardPool(pool);
                    }
                }
            }

            // MegaPool
            AddVanillaCardPool(allGameData.FindRelicData(/*Large Mystery Box*/"d537c198-899f-4385-9552-f35f58826be4")?.GetFirstRelicEffectData<RelicEffectAddPrecachedBattleCardToHand>()?.GetParamCardPool());
            // UnitsAllBanner
            AddVanillaCardPool((allGameData.FindRewardData(/*CardDraftLevelUpUnitMainOrAllied*/"8a8782bf-3faf-4dd7-8f5d-413c1b269c3d") as DraftRewardData)?.GetDraftPool());
            // StarterCardsOnly:
            filter = allGameData.FindPyreArtifactData(/*Dominion Pyre*/"c0c09adc-b23e-422e-bb75-689ee82cfa36")?.GetFirstRelicEffectData<RelicEffectPurgeStarterDeckMatchingFilter>()?.GetParamCardFilter();
            AddVanillaCardPoolsFromFilter(filter);
            // NewChallengerChampionPool
            AddVanillaCardPool(allGameData.FindMutatorData(/*ANewChallenger*/"8bf5c01f-cd2a-46d2-8ba5-c4f594bf2753")?.GetFirstRelicEffectData<RelicEffectReplaceChampion>()?.GetParamCardPool());
            // RoomAndEquipmentDraftPool
            AddVanillaCardPool((allGameData.FindRewardData(/*CardDraftEquipmentOrRoom*/ "4a74d384-8d5b-4c62-9758-eccd50f31f4a") as DraftRewardData)?.GetDraftPool());
            // RoomAndEquipmentMerchant_EquipmentPool
            // RoomAndEquipmentMerchant_RoomPool
            // RoomAndEquipmentMerchant_RoomAndEquipmentPool
            for (int i = 0; i < 3; i++)
            {
                cardPoolReward = merchant?.GetReward(i)?.RewardData;
                if (cardPoolReward != null)
                    AddVanillaCardPool(cardPoolRewardDataField.GetValue(cardPoolReward) as CardPool);
            }
            // EquipmentPoolNonEnemy
            filter = swordmaiden?.GetTriggers()[0]?.GetEffects()[0]?.GetParamCardFilter();
            AddVanillaCardPoolsFromFilter(filter);
            // EquipmentPoolEnemy    
            filter = swordmaiden?.GetTriggers()[0]?.GetEffects()[0]?.GetParamCardFilterSecondary();
            AddVanillaCardPoolsFromFilter(filter);
            // FreeEquipmentMutatorPool
            AddVanillaCardPool(allGameData.FindCardData(/*Fetch Ability*/ "8d54f68b-5660-4507-84c3-a0cf092bfbf1")?.GetEffects()[0]?.GetParamCardPool());
            // RedCrownEquipmentPool
            AddVanillaCardPool(allGameData.FindCardUpgradeData(/*CultOfTheLambRedCrownUpgrade*/ "6bf9552b-c6d8-43b9-ad85-4e596bed591d")?.GetCharacterTriggerUpgrades()[0]?.GetEffects()[0]?.GetParamCardPool());
            // RandomGraftedEquipmentForEvent_NoVoidArms_EquipmentPool
            var ergreward = allGameData.FindRewardData(/*LazarusLeagueLabRandomGraftReward*/ "8d1a2f34-8266-494c-86a1-77bee44b949b") as EquipmentRandomGraftRewardData;
            AddVanillaCardPool(equipmentGraftRewardDataField.GetValue(ergreward) as CardPool);
            // TrainRoomPool
            AddVanillaCardPool(allGameData.FindMutatorData(/*FreeRealEstate*/ "a6ef5ab8-1701-4488-aae2-681afe241490")?.GetFirstRelicEffectData<RelicEffectAddCardsStartOfRun>()?.GetParamCardPool() as CardPool);
        }
    }
}
