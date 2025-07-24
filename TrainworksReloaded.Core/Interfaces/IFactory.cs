namespace TrainworksReloaded.Core.Interfaces
{
    public interface IFactory<T>
    {
        public string FactoryKey { get; }
        T? GetValue();
    }
}
