using HarmonyLib;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Subtype
{
    public class SubtypeDataPipeline : IDataPipeline<IRegister<SubtypeData>, SubtypeData>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> termRegister;

        public SubtypeDataPipeline(PluginAtlas atlas, IRegister<LocalizationTerm> termRegister)
        {
            this.atlas = atlas;
            this.termRegister = termRegister;
        }

        public List<IDefinition<SubtypeData>> Run(IRegister<SubtypeData> service)
        {
            var processList = new List<IDefinition<SubtypeData>>();
            foreach (var definition in atlas.PluginDefinitions)
            {
                var key = definition.Key;
                var pluginConfig = definition.Value.Configuration;
                foreach (var config in pluginConfig.GetSection("subtypes").GetChildren())
                {
                    var id = config.GetSection("id").ParseString();
                    if (id == null)
                    {
                        continue;
                    }

                    var text = config.GetSection("names").ParseLocalizationTerm();
                    if (text == null)
                    {
                        continue;
                    }

                    var name = key.GetId(TemplateConstants.Subtype, id);
                    var nameKey = $"SubtytpesData_nameKey-{name}";
                    var subtypeData = new SubtypeData();

                    text.Key = nameKey;
                    termRegister.Add(nameKey, text);

                    AccessTools.Field(typeof(SubtypeData), "_subtype").SetValue(subtypeData, nameKey);

                    service.Register(name, subtypeData);

                    processList.Add(new SubtypeDefinition(key, subtypeData, config)
                    {
                        Id = id
                    });
                }
            }
            return processList;
        }
    }
}
