using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Map
{
    public class RewardNodeDataFactory : IFactory<MapNodeData>
    {
        public string FactoryKey => "reward";

        public MapNodeData? GetValue()
        {
            return ScriptableObject.CreateInstance<RewardNodeData>();
        }
    }
}
