using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicPoolPipeline : IDataPipeline<IRegister<RelicPool>, RelicPool>
    {
        private readonly PluginAtlas atlas;
        private readonly IInstanceGenerator<RelicPool> generator;

        public RelicPoolPipeline(
            PluginAtlas atlas,
            IInstanceGenerator<RelicPool> generator
        )
        {
            this.atlas = atlas;
            this.generator = generator;
        }

        public List<IDefinition<RelicPool>> Run(IRegister<RelicPool> service)
        {
            var processList = new List<IDefinition<RelicPool>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadPools(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        public List<RelicPoolDefinition> LoadPools(
            IRegister<RelicPool> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<RelicPoolDefinition>();
            foreach (var child in pluginConfig.GetSection("relic_pools").GetChildren())
            {
                var data = LoadRelicPoolConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public RelicPoolDefinition? LoadRelicPoolConfiguration(
            IRegister<RelicPool> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.RelicPool, id);
            var data = generator.CreateInstance();
            data.name = name;
            service.Register(name, data);

            return new RelicPoolDefinition(key, data, configuration) { Id = id };
        }
    }
}
