using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class SoulDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "soul";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<SoulData>();
        }
    }
}