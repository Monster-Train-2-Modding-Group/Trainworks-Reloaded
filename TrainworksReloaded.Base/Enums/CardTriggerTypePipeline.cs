using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class CardTriggerTypePipeline
        : IDataPipeline<IRegister<CardTriggerType>, CardTriggerType>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> termRegister;

        public CardTriggerTypePipeline(PluginAtlas atlas, IRegister<LocalizationTerm> termRegister)
        {
            this.atlas = atlas;
            this.termRegister = termRegister;
        }

        public List<IDefinition<CardTriggerType>> Run(IRegister<CardTriggerType> service)
        {
            var processList = new List<IDefinition<CardTriggerType>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadTriggers(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<IDefinition<CardTriggerType>> LoadTriggers(
            IRegister<CardTriggerType> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<IDefinition<CardTriggerType>>();
            foreach (var child in pluginConfig.GetSection("card_trigger_types").GetChildren())
            {
                var data = LoadTriggerConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CardTriggerTypeDefinition? LoadTriggerConfiguration(
            IRegister<CardTriggerType> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId(TemplateConstants.CardTriggerEnum, id);
            CardTriggerType trigger = EnumAllocator<CardTriggerType>.CreateEnum(key, id);

            // The localization keys per trigger are generated based a based on a base key name.
            var baseKey = "CardTrigger_" + id;

            var localizationNameTerm = configuration.GetSection("names").ParseLocalizationTerm();
            if (localizationNameTerm != null)
            {
                localizationNameTerm.Key = baseKey + "_CardText";
                termRegister.Register(localizationNameTerm.Key, localizationNameTerm);
            }

            var localizationCharacterTooltipTerm = configuration.GetSection("descriptions").ParseLocalizationTerm();
            if (localizationCharacterTooltipTerm != null)
            {
                localizationCharacterTooltipTerm.Key = baseKey + "_TooltipText";
                termRegister.Register(localizationCharacterTooltipTerm.Key, localizationCharacterTooltipTerm);
            }

            service.Register(name, trigger);
            return new CardTriggerTypeDefinition(key, trigger, configuration)
            {
                Id = id,
            };
        }
    }
}