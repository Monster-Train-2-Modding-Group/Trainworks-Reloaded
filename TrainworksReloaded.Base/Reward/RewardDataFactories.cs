using System;
using System.Collections.Generic;
using System.Text;
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

    public class CardRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "card";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<CardRewardData>();
        }
    }

    public class DraftRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "draft";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<DraftRewardData>();
        }
    }

    public class EnhancerRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "enhancer";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<EnhancerRewardData>();
        }
    }

    public class GoldRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "gold";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<GoldRewardData>();
        }
    }

    public class HealthRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "health";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<HealthRewardData>();
        }
    }

    public class PurgeRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "purge";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<PurgeRewardData>();
        }
    }

    public class RelicDraftRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "relic_draft";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<RelicDraftRewardData>();
        }
    }

    public class RelicRewardDataFactory : IFactory<RewardData>
    {
        public string FactoryKey => "relic";

        public RewardData? GetValue()
        {
            return ScriptableObject.CreateInstance<RelicRewardData>();
        }
    }
}
