using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine.UIElements;

namespace TrainworksReloaded.Base.Reward
{
    public class RewardDataRegister : Dictionary<string, RewardData>, IRegister<RewardData>
    {
        private readonly IModLogger<CardPoolRegister> logger;
        private readonly Lazy<List<GrantableRewardData>> Rewards;

        public RewardDataRegister(GameDataClient client, IModLogger<CardPoolRegister> logger)
        {
            this.logger = logger;
            Rewards = new Lazy<List<GrantableRewardData>>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    var rewards = AccessTools.Field(typeof(AllGameData), "rewardDatas").GetValue(provider.GetAllGameData()) as List<GrantableRewardData>;
                    return rewards ?? [];
                }
                else
                {
                    return [];
                }
            });
        }

        public void Register(string key, RewardData item)
        {
            logger.Log(LogLevel.Debug, $"Register Reward {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }
        
        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out RewardData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        return true;
                    }
                    foreach (var reward in Rewards.Value)
                    {
                        if (reward.GetAssetKey() == identifier)
                        {
                            IsModded = false;
                            lookup = reward;
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        return true;
                    }
                    foreach (var reward in Rewards.Value)
                    {
                        if (reward.GetID() == identifier)
                        {
                            IsModded = false;
                            lookup = reward;
                            return true;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

    }
}
