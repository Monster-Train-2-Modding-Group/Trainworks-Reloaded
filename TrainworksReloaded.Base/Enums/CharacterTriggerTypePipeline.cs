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
    public class CharacterTriggerTypePipeline
        : IDataPipeline<IRegister<CharacterTriggerData.Trigger>, CharacterTriggerData.Trigger>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<LocalizationTerm> termRegister;
        private static int NextEnumId = (from int x in Enum.GetValues(typeof(CharacterTriggerData.Trigger)).AsQueryable() select x).Max() + 1;

        public CharacterTriggerTypePipeline(PluginAtlas atlas, IRegister<LocalizationTerm> termRegister)
        {
            this.atlas = atlas;
            this.termRegister = termRegister;
        }

        public List<IDefinition<CharacterTriggerData.Trigger>> Run(IRegister<CharacterTriggerData.Trigger> service)
        {
            var processList = new List<IDefinition<CharacterTriggerData.Trigger>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadTriggers(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<IDefinition<CharacterTriggerData.Trigger>> LoadTriggers(
            IRegister<CharacterTriggerData.Trigger> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<IDefinition<CharacterTriggerData.Trigger>>();
            foreach (var child in pluginConfig.GetSection("character_trigger_types").GetChildren())
            {
                var data = LoadTriggerConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CharacterTriggerTypeDefinition? LoadTriggerConfiguration(
            IRegister<CharacterTriggerData.Trigger> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId(TemplateConstants.CharacterTriggerEnum, id);
            CharacterTriggerData.Trigger trigger = (CharacterTriggerData.Trigger)NextEnumId++;

            // The localization keys per trigger are generated based a based on a base key name.
            var baseKey = "CharacterTrigger_" + id;

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

            var localizationNotificationTerm = configuration.GetSection("notifications").ParseLocalizationTerm();
            if (localizationNotificationTerm != null)
            {
                localizationNotificationTerm.Key = baseKey + "_NotificationText";
                termRegister.Register(localizationNotificationTerm.Key, localizationNotificationTerm);
            }

            service.Register(name, trigger);
            return new CharacterTriggerTypeDefinition(key, trigger, configuration)
            {
                Id = id,
            };
        }
    }
}