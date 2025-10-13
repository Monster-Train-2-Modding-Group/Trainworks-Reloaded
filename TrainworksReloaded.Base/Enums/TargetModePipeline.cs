using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using static TargetHelper;

namespace TrainworksReloaded.Base.Enums
{
    public class TargetModePipeline : IDataPipeline<IRegister<TargetMode>, TargetMode>
    {
        private readonly PluginAtlas atlas;
        private static byte NextEnumId = (byte) ((from byte x in Enum.GetValues(typeof(TargetMode)).AsQueryable() select x).Max() + 1);

        public TargetModePipeline(PluginAtlas atlas)
        {
            this.atlas = atlas;
        }

        public List<IDefinition<TargetMode>> Run(IRegister<TargetMode> service)
        {
            foreach (var config in atlas.PluginDefinitions)
            {
                LoadTargetModes(service, config.Key, config.Value.Configuration);
            }
            return [];
        }

        private void LoadTargetModes(IRegister<TargetMode> service, string key, IConfiguration pluginConfig)
        {
            foreach (var child in pluginConfig.GetSection("target_modes").GetChildren())
            {
                LoadConfiguration(service, key, child);
            }
        }

        private void LoadConfiguration(IRegister<TargetMode> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return;
            }

            var name = key.GetId(TemplateConstants.TargetModeEnum, id);
            TargetMode targetMode = (TargetMode)NextEnumId++;
            service.Register(name, targetMode);
        }
    }
}