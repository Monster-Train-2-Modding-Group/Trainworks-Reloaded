using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Reward
{
    public class CardPoolRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "cardpool";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<CardPoolRewardData>();
        }
    }
}
