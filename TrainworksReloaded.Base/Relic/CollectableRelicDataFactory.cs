using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class CollectableRelicDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "collectable";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<CollectableRelicData>();
        }
    }
}