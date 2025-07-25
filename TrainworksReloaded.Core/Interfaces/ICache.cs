namespace TrainworksReloaded.Core.Interfaces
{
    public interface ICache<T>
    {
        public void AddToCache(T item);
        public IEnumerable<T> GetCacheItems();
        public void Clear();
    }
}
