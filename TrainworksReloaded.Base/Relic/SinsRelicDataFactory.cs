using System;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class SinsDataFactory : IFactory<RelicData>
    {
        public string FactoryKey => "sin";

        public RelicData? GetValue()
        {
            return ScriptableObject.CreateInstance<SinsData>();
        }
    }
} 