using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class StatusEffectTriggerStagePipeline : IDataPipeline<IRegister<StatusEffectData.TriggerStage>, StatusEffectData.TriggerStage>
    {
        private readonly PluginAtlas atlas;

        public StatusEffectTriggerStagePipeline(PluginAtlas atlas)
        {
            this.atlas = atlas;
        }

        public List<IDefinition<StatusEffectData.TriggerStage>> Run(IRegister<StatusEffectData.TriggerStage> service)
        {
            foreach (var config in atlas.PluginDefinitions)
            {
                LoadTriggerStages(service, config.Key, config.Value.Configuration);
            }
            return [];
        }

        private void LoadTriggerStages(IRegister<StatusEffectData.TriggerStage> service, string key, IConfiguration pluginConfig)
        {
            foreach (var child in pluginConfig.GetSection("status_effect_trigger_stages").GetChildren())
            {
                LoadConfiguration(service, key, child);
            }
        }

        private void LoadConfiguration(IRegister<StatusEffectData.TriggerStage> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return;
            }

            var name = key.GetId(TemplateConstants.StatusEffectTriggerStageEnum, id);
            StatusEffectData.TriggerStage triggerStage = EnumAllocator<StatusEffectData.TriggerStage>.CreateEnum(key, id);
            service.Register(name, triggerStage);
        }
    }
}