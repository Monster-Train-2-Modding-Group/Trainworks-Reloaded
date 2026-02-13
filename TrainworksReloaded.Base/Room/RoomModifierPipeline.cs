using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Room
{
    public class RoomModifierPipeline : IDataPipeline<IRegister<RoomModifierData>, RoomModifierData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<RoomModifierPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;

        public RoomModifierPipeline(
            PluginAtlas atlas,
            IModLogger<RoomModifierPipeline> logger,
            IRegister<LocalizationTerm> termRegister
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
        }

        public List<IDefinition<RoomModifierData>> Run(IRegister<RoomModifierData> service)
        {
            var processList = new List<IDefinition<RoomModifierData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(
                    LoadRoomModifiers(service, config.Key, config.Value.Configuration)
                );
            }
            return processList;
        }

        private List<RoomModifierDefinition> LoadRoomModifiers(
            IRegister<RoomModifierData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<RoomModifierDefinition>();
            foreach (var child in pluginConfig.GetSection("room_modifiers").GetChildren())
            {
                var data = LoadRoomModifier(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private RoomModifierDefinition? LoadRoomModifier(
            IRegister<RoomModifierData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId("RoomModifier", id);
            var titleKey = $"RoomModifierData_titleKey-{name}";
            var descriptionKey = $"RoomModifierData_descriptionKey-{name}";
            var descriptionKeyInPlay = $"RoomModifierData_descriptionKeyInPlay-{name}";
            var extraTooltipTitleKey = $"RoomModifierData_extraTooltipTitleKey-{name}";
            var extraTooltipBodyKey = $"RoomModifierData_extraTooltipBodyKey-{name}";
            var data = new RoomModifierData();

            //handle names
            var nameClass = configuration.GetSection("name").ParseReference();
            if (nameClass == null)
                return null;

            var modReference = nameClass.mod_reference ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !nameClass.id.GetFullyQualifiedName<RoomStateModifierBase>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                logger.Log(LogLevel.Error, $"Failed to load room modifier state name {nameClass} in {id} mod {modReference}, Make sure the class exists in {modReference} and that the class inherits from RoomStateModifierBase.");
                return null;
            }
            AccessTools
                .Field(typeof(RoomModifierData), "roomStateModifierClassName")
                .SetValue(data, fullyQualifiedName);


            var titleKeyTerm = configuration.GetSection("titles").ParseLocalizationTerm();
            if (titleKeyTerm != null)
            {
                AccessTools.Field(typeof(RoomModifierData), "overrideTitleKey").SetValue(data, titleKey);
                titleKeyTerm.Key = titleKey;
                termRegister.Register(titleKey, titleKeyTerm);
            }

            //handle descriptions
            var descriptionKeyTerm = configuration
                .GetSection("descriptions")
                .ParseLocalizationTerm();
            if (descriptionKeyTerm != null)
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "descriptionKey")
                    .SetValue(data, descriptionKey);
                descriptionKeyTerm.Key = descriptionKey;
                termRegister.Register(descriptionKey, descriptionKeyTerm);
            }

            //handle descriptions
            var descriptionKeyInPlayTerm = configuration
                .GetDeprecatedSection("play_descriptions", "in_play_descriptions")
                .ParseLocalizationTerm();
            if (descriptionKeyInPlayTerm != null)
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "descriptionKeyInPlay")
                    .SetValue(data, descriptionKeyInPlay);
                descriptionKeyInPlayTerm.Key = descriptionKeyInPlay;
                termRegister.Register(descriptionKeyInPlay, descriptionKeyInPlayTerm);
            }

            //handle descriptions
            var extraTooltipTitleKeyTerm = configuration
                .GetSection("extra_title_tooltips")
                .ParseLocalizationTerm();
            if (extraTooltipTitleKeyTerm != null)
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "extraTooltipTitleKey")
                    .SetValue(data, extraTooltipTitleKey);
                extraTooltipTitleKeyTerm.Key = extraTooltipTitleKey;
                termRegister.Register(extraTooltipTitleKey, extraTooltipTitleKeyTerm);
            }
            else
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "extraTooltipTitleKey")
                    .SetValue(data, string.Empty);
            }

            //handle descriptions
            var extraTooltipBodyKeyTerm = configuration
                .GetSection("extra_body_tooltips")
                .ParseLocalizationTerm();
            if (extraTooltipBodyKeyTerm != null)
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "extraTooltipBodyKey")
                    .SetValue(data, extraTooltipBodyKey);
                extraTooltipBodyKeyTerm.Key = extraTooltipBodyKey;
                termRegister.Register(extraTooltipBodyKey, extraTooltipBodyKeyTerm);
            }
            else
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "extraTooltipBodyKey")
                    .SetValue(data, string.Empty);
            }

            //bool
            var useTitleForCardDescription = false;
            AccessTools
                .Field(typeof(RoomModifierData), "useTitleForCardDescription")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("use_name_as_description", "use_title_for_card_description").ParseBool()
                        ?? useTitleForCardDescription
                );

            var disallowInDeploymentPhase = false;
            AccessTools
                .Field(typeof(RoomModifierData), "disallowedInDeploymentPhase")
                .SetValue(
                    data,
                    configuration.GetSection("disallow_in_deployment").ParseBool()
                        ?? disallowInDeploymentPhase
                );

            //int
            var paramInt = 0;
            AccessTools
                .Field(typeof(RoomModifierData), "paramInt")
                .SetValue(data, configuration.GetSection("param_int").ParseInt() ?? paramInt);

            var paramInt2 = 0;
            AccessTools
                .Field(typeof(RoomModifierData), "paramInt2")
                .SetValue(data, configuration.GetSection("param_int_2").ParseInt() ?? paramInt2);

            var paramFloat = 0f;
            AccessTools
                .Field(typeof(RoomModifierData), "paramFloat")
                .SetValue(data, configuration.GetSection("param_float").ParseFloat() ?? paramFloat);

            var tooltipsSuppressed = false;
            AccessTools
                .Field(typeof(RoomModifierData), "tooltipsSuppressed")
                .SetValue(data, configuration.GetSection("tooltips_suppressed").ParseBool() ?? tooltipsSuppressed);

            service.Register(name, data);
            return new RoomModifierDefinition(key, data, configuration) { Id = id };
        }
    }
}
