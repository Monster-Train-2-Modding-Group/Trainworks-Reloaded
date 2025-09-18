using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trigger
{
    public class CardTriggerEffectPipeline
        : IDataPipeline<IRegister<CardTriggerEffectData>, CardTriggerEffectData>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> termRegister;

        public CardTriggerEffectPipeline(
            PluginAtlas atlas,
            IRegister<LocalizationTerm> termRegister
        )
        {
            this.atlas = atlas;
            this.termRegister = termRegister;
        }

        public List<IDefinition<CardTriggerEffectData>> Run(
            IRegister<CardTriggerEffectData> service
        )
        {
            var processList = new List<IDefinition<CardTriggerEffectData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadTriggers(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<CardTriggerEffectDefinition> LoadTriggers(
            IRegister<CardTriggerEffectData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CardTriggerEffectDefinition>();
            foreach (var child in pluginConfig.GetSection("card_triggers").GetChildren())
            {
                var data = LoadTriggerConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CardTriggerEffectDefinition? LoadTriggerConfiguration(
            IRegister<CardTriggerEffectData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId(TemplateConstants.CardTrigger, id);
            var descriptionKey = $"CardTriggerEffectData_descriptionKey-{name}";

            var data = new CardTriggerEffectData();

            //handle descriptions
            var localizationDescription = configuration
                .GetSection("descriptions")
                .ParseLocalizationTerm();
            if (localizationDescription != null)
            {
                AccessTools
                    .Field(typeof(CardTriggerEffectData), "descriptionKey")
                    .SetValue(data, descriptionKey);
                localizationDescription.Key = descriptionKey;
                termRegister.Register(descriptionKey, localizationDescription);
            }

            var triggerKeywordTooltipSuppressed = configuration.GetSection("trigger_keyword_tooltip_suppressed").ParseBool() ?? false;
            AccessTools
                .Field(typeof(CardTriggerEffectData), "triggerKeywordTooltipSuppressed")
                .SetValue(data, triggerKeywordTooltipSuppressed);

            service.Register(name, data);
            return new CardTriggerEffectDefinition(key, data, configuration) { Id = id };
        }
    }
}
