using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class SoulPoolPipeline : IDataPipeline<IRegister<SoulPool>, SoulPool>
    {
        private readonly PluginAtlas atlas;
        private readonly IInstanceGenerator<SoulPool> generator;

        public SoulPoolPipeline(
            PluginAtlas atlas,
            IInstanceGenerator<SoulPool> generator
        )
        {
            this.atlas = atlas;
            this.generator = generator;
        }

        public List<IDefinition<SoulPool>> Run(IRegister<SoulPool> service)
        {
            var processList = new List<IDefinition<SoulPool>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadPools(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        public List<SoulPoolDefinition> LoadPools(
            IRegister<SoulPool> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<SoulPoolDefinition>();
            foreach (var child in pluginConfig.GetSection("soul_pools").GetChildren())
            {
                var data = LoadSoulPoolConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public SoulPoolDefinition? LoadSoulPoolConfiguration(
            IRegister<SoulPool> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.SoulPool, id);
            var data = generator.CreateInstance();
            data.name = name;
            service.Register(name, data);

            return new SoulPoolDefinition(key, data, configuration) { Id = id };
        }
    }
}
