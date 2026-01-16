using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class EndlessMutatorDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "endless_mutator";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<EndlessMutatorData>();
        }
    }
}