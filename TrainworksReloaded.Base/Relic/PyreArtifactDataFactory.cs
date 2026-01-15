using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class PyreArtifactDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "pyre_artifact";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<PyreArtifactData>();
        }
    }
}