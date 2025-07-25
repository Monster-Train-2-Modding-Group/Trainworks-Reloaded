using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Core.Impl
{
    public class CacheDataPipelineDecorator<T, U> : IDataPipeline<T, U>
        where T : IRegister<U>
    {
        private readonly IDataPipeline<T, U> decoratee;
        private readonly ICache<IDefinition<U>> cache;

        public CacheDataPipelineDecorator(
            IDataPipeline<T, U> decoratee,
            ICache<IDefinition<U>> cache
        )
        {
            this.decoratee = decoratee;
            this.cache = cache;
        }

        public List<IDefinition<U>> Run(T service)
        {
            var definitions = decoratee.Run(service);
            foreach (var definition in definitions)
            {
                cache.AddToCache(definition);
            }
            return definitions;
        }
    }
}
