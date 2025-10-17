using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Class
{
    public class ClassCardStylePipeline
        : IDataPipeline<IRegister<ClassCardStyle>, ClassCardStyle>
    {
        private readonly PluginAtlas atlas;
        private static int NextEnumId = (from int x in Enum.GetValues(typeof(ClassCardStyle)).AsQueryable() select x).Max() + 1;

        public ClassCardStylePipeline(PluginAtlas atlas)
        {
            this.atlas = atlas;
        }

        public List<IDefinition<ClassCardStyle>> Run(IRegister<ClassCardStyle> service)
        {
            var processList = new List<IDefinition<ClassCardStyle>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadClassCardStyles(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<IDefinition<ClassCardStyle>> LoadClassCardStyles(
            IRegister<ClassCardStyle> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<IDefinition<ClassCardStyle>>();
            foreach (var child in pluginConfig.GetSection("class_card_styles").GetChildren())
            {
                var data = LoadConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private ClassCardStyleDefinition? LoadConfiguration(
            IRegister<ClassCardStyle> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId(TemplateConstants.ClassCardStyle, id);
            ClassCardStyle classCardStyle = (ClassCardStyle)(NextEnumId++);

            service.Register(name, classCardStyle);
            return new ClassCardStyleDefinition(key, classCardStyle, configuration)
            {
                Id = id,
            };
        }
    }
}