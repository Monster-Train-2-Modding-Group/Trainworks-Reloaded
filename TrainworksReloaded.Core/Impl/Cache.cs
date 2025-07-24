using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Core.Impl
{
    public class Cache<T> : ICache<T>
    {
        public List<T> Values { get; set; } = new List<T>();

        public void AddToCache(T item)
        {
            Values.Add(item);
        }

        public void Clear()
        {
            Values.Clear();
        }

        public IEnumerable<T> GetCacheItems()
        {
            return Values.AsEnumerable();
        }
    }
}
