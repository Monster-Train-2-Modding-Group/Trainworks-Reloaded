using HarmonyLib;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Reward
{
    public class RewardDataRegister : Dictionary<string, RewardData>, IRegister<RewardData>
    {
        private readonly IModLogger<CardPoolRegister> logger;
        private readonly Lazy<List<GrantableRewardData>> Rewards;
        private readonly Dictionary<string, RewardData> VanillaRewards = [];

        public RewardDataRegister(GameDataClient client, IModLogger<CardPoolRegister> logger)
        {
            this.logger = logger;
            VanillaRewards.AddRange(Resources.FindObjectsOfTypeAll<RewardData>().ToDictionary(x => x.name, x => x));
            this.AddRange(VanillaRewards);
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
            if (item is GrantableRewardData grantableRewardData) 
                Rewards.Value.Add(grantableRewardData);
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
            IsModded = false;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    IsModded = !VanillaRewards.ContainsKey(identifier);
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        IsModded = !VanillaRewards.ContainsKey(identifier);
                        return true;
                    }
                    foreach (var reward in this.Values)
                    {
                        if (reward.GetID() == identifier)
                        {
                            IsModded = !VanillaRewards.ContainsKey(reward.name);
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
