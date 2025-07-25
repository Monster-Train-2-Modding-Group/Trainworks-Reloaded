using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Core.Impl
{
    public class InstanceGenerator<T> : IInstanceGenerator<T>
        where T : new()
    {
        public string Key { get; } = "InstanceGenerator";

        public T CreateInstance()
        {
            return new T();
        }
    }
}
