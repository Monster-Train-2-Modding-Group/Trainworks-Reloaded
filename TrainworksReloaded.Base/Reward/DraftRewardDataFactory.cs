using System;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Reward
{
    public class DraftRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "draft";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<DraftRewardData>();
        }
    }
} 