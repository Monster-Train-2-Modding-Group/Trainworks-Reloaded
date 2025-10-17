using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class TrackedValueTypePipeline : IDataPipeline<IRegister<CardStatistics.TrackedValueType>, CardStatistics.TrackedValueType>
    {
        private readonly PluginAtlas atlas;
        private static int NextEnumId = (int) ((from int x in Enum.GetValues(typeof(CardStatistics.TrackedValueType)).AsQueryable() select x).Max() + 1);

        public TrackedValueTypePipeline(PluginAtlas atlas)
        {
            this.atlas = atlas;
        }

        public List<IDefinition<CardStatistics.TrackedValueType>> Run(IRegister<CardStatistics.TrackedValueType> service)
        {
            foreach (var config in atlas.PluginDefinitions)
            {
                LoadTrackedValueTypes(service, config.Key, config.Value.Configuration);
            }
            return [];
        }

        private void LoadTrackedValueTypes(IRegister<CardStatistics.TrackedValueType> service, string key, IConfiguration pluginConfig)
        {
            foreach (var child in pluginConfig.GetSection("tracked_value_types").GetChildren())
            {
                LoadConfiguration(service, key, child);
            }
        }

        private void LoadConfiguration(IRegister<CardStatistics.TrackedValueType> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return;
            }

            var name = key.GetId(TemplateConstants.TrackedValueTypeEnum, id);
            CardStatistics.TrackedValueType trackedValue = (CardStatistics.TrackedValueType)NextEnumId++;
            service.Register(name, trackedValue);
        }
    }
}