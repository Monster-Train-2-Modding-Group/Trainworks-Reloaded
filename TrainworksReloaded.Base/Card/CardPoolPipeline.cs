using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Card
{
    public class CardPoolPipeline : IDataPipeline<IRegister<CardPool>, CardPool>
    {
        private readonly PluginAtlas atlas;
        private readonly IInstanceGenerator<CardPool> generator;

        public CardPoolPipeline(
            PluginAtlas atlas,
            IRegister<LocalizationTerm> termRegister,
            IInstanceGenerator<CardPool> generator
        )
        {
            this.atlas = atlas;
            this.generator = generator;
        }

        public List<IDefinition<CardPool>> Run(IRegister<CardPool> service)
        {
            var processList = new List<IDefinition<CardPool>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadPools(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        public List<CardPoolDefinition> LoadPools(
            IRegister<CardPool> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CardPoolDefinition>();
            foreach (var child in pluginConfig.GetSection("card_pools").GetChildren())
            {
                var data = LoadCardPoolConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        public CardPoolDefinition? LoadCardPoolConfiguration(
            IRegister<CardPool> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.CardPool, id);
            var data = generator.CreateInstance();
            data.name = name;
            service.Register(name, data);

            return new CardPoolDefinition(key, data, configuration) { Id = id };
        }
    }
}
