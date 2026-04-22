using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class CovenantDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "covenant";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<CovenantData>();
        }
    }
}