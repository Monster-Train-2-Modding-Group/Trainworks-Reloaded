using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base
{
    public class ScriptableObjectInstanceGenerator<T> : IInstanceGenerator<T>
        where T : ScriptableObject, new()
    {
        public T CreateInstance()
        {
            return ScriptableObject.CreateInstance<T>();
        }
    }
}
