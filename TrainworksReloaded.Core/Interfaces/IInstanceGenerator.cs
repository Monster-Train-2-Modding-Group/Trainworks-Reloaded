namespace TrainworksReloaded.Core.Interfaces
{
    public interface IInstanceGenerator<T>
        where T : new()
    {
        T CreateInstance();
    }
}
