using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerPoolPipeline : IDataPipeline<IRegister<EnhancerPool>, EnhancerPool>
    {
        private readonly PluginAtlas atlas;
        private readonly IInstanceGenerator<EnhancerPool> generator;

        public EnhancerPoolPipeline(
            PluginAtlas atlas,
            IInstanceGenerator<EnhancerPool> generator
        )
        {
            this.atlas = atlas;
            this.generator = generator;
        }

        public List<IDefinition<EnhancerPool>> Run(IRegister<EnhancerPool> service)
        {
            var processList = new List<IDefinition<EnhancerPool>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadPools(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        public List<EnhancerPoolDefinition> LoadPools(
            IRegister<EnhancerPool> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<EnhancerPoolDefinition>();
            foreach (var child in pluginConfig.GetSection("enhancer_pools").GetChildren())
            {
                var data = LoadEnhancerPoolConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public EnhancerPoolDefinition? LoadEnhancerPoolConfiguration(
            IRegister<EnhancerPool> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.EnhancerPool, id);
            var data = generator.CreateInstance();
            data.name = name;
            service.Register(name, data);

            return new EnhancerPoolDefinition(key, data, configuration) { Id = id };
        }
    }
}
