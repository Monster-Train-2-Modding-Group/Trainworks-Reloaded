using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class TooltipDesignTypePipeline : IDataPipeline<IRegister<TooltipDesigner.TooltipDesignType>, TooltipDesigner.TooltipDesignType>
    {
        private readonly PluginAtlas atlas;

        public TooltipDesignTypePipeline(PluginAtlas atlas)
        {
            this.atlas = atlas;
        }

        public List<IDefinition<TooltipDesigner.TooltipDesignType>> Run(IRegister<TooltipDesigner.TooltipDesignType> service)
        {
            List<IDefinition<TooltipDesigner.TooltipDesignType>> ret = [];
            foreach (var config in atlas.PluginDefinitions)
            {
                var list = LoadItems(service, config.Key, config.Value.Configuration);
                ret.AddRange(list);
            }
            return ret;
        }

        private List<IDefinition<TooltipDesigner.TooltipDesignType>> LoadItems(IRegister<TooltipDesigner.TooltipDesignType> service, string key, IConfiguration pluginConfig)
        {
            List<IDefinition<TooltipDesigner.TooltipDesignType>> ret = [];
            foreach (var child in pluginConfig.GetSection("tooltip_design_types").GetChildren())
            {
                var definition = LoadConfiguration(service, key, child);
                if (definition != null)
                    ret.Add(definition);
            }
            return ret;
        }

        private IDefinition<TooltipDesigner.TooltipDesignType>? LoadConfiguration(IRegister<TooltipDesigner.TooltipDesignType> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.TooltipDesignTypeEnum, id);
            TooltipDesigner.TooltipDesignType designType = EnumAllocator<TooltipDesigner.TooltipDesignType>.CreateEnum(key, id);
            service.Register(name, designType);

            return new TooltipDesignTypeDefinition(key, designType, configuration)
            {
                Id = id
            };
        }
    }
}