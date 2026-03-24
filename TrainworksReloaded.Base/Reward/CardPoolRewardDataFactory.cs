using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Reward
{
    public class CardPoolRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "card_pool";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<CardPoolRewardData>();
        }
    }
}
