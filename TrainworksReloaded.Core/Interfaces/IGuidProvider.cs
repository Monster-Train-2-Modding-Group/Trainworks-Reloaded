namespace TrainworksReloaded.Core.Interfaces
{
    public interface IGuidProvider
    {
        Guid GetGuidDeterministic(string key);
    }
}
