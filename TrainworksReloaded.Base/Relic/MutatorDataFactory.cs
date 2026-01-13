using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class MutatorDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "mutator";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<MutatorData>();
        }
    }
}