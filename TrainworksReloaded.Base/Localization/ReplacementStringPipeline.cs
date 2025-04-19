using HarmonyLib;
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
    public class ReplacementStringPipeline : IDataPipeline<IRegister<ReplacementStringData>, ReplacementStringData>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> locService;

        public ReplacementStringPipeline(PluginAtlas atlas, IRegister<LocalizationTerm> locService)
        {
            this.atlas = atlas;
            this.locService = locService;
        }

        public List<IDefinition<ReplacementStringData>> Run(IRegister<ReplacementStringData> service)
        {
            var processList = new List<IDefinition<ReplacementStringData>>();
            foreach (var definition in atlas.PluginDefinitions)
            {
                var pluginConfig = definition.Value.Configuration;
                var key = definition.Key;
                foreach (var config in pluginConfig.GetSection("replacement_texts").GetChildren())
                {
                    var id = config.GetSection("key").ParseString();
                    if (id == null)
                    {
                        continue;
                    }

                    var replacement_key = $"{key}_{id}";
                    var loc_key = $"ReplacementStringsData_replacement-{replacement_key}";

                    var text = config.GetSection("texts").ParseLocalizationTerm();
                    if (text == null)
                    {
                        continue;
                    }

                    text.Key = loc_key;
                    locService.Register(text.Key, text);

                    ReplacementStringData replacement = new();
                    AccessTools.Field(typeof(ReplacementStringData), "_keyword").SetValue(replacement, replacement_key);
                    AccessTools.Field(typeof(ReplacementStringData), "_replacement").SetValue(replacement, text.Key);
                    service.Register(replacement_key, replacement);
                }
            }
            return processList;
        }
    }
}
