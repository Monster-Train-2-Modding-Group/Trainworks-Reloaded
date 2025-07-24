using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "enhancer";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<EnhancerData>();
        }
    }
}