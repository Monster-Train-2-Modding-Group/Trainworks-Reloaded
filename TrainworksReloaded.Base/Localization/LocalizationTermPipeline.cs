using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Room;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Localization
{
    public class LocalizationTermPipeline : IDataPipeline<IRegister<LocalizationTerm>, LocalizationTerm>
    {
        private readonly PluginAtlas atlas;

        public LocalizationTermPipeline(PluginAtlas atlas)
        {
            this.atlas = atlas;
        }

        public List<IDefinition<LocalizationTerm>> Run(IRegister<LocalizationTerm> service)
        {
            var processList = new List<IDefinition<LocalizationTerm>>();
            foreach (var definition in atlas.PluginDefinitions)
            {
                var pluginConfig = definition.Value.Configuration;
                foreach (var config in pluginConfig.GetSection("localization_terms").GetChildren())
                {
                    var key = config.GetSection("key").ParseString();
                    if (key == null)
                    {
                        continue;
                    }

                    var text = config.GetSection("texts").ParseLocalizationTerm();
                    if (text == null)
                    {
                        continue;
                    }

                    text.Key = key;
                    service.Add(key, text);
                }
            }
            return processList;
        }
    }
}
