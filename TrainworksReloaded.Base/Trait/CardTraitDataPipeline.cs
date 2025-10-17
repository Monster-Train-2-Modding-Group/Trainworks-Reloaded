using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trait
{
    public class CardTraitDataPipeline : IDataPipeline<IRegister<CardTraitData>, CardTraitData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<CardTraitDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> localizationService;

        public CardTraitDataPipeline(
            PluginAtlas atlas,
            IModLogger<CardTraitDataPipeline> logger,
            IRegister<LocalizationTerm> localizationService
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.localizationService = localizationService;
        }

        public List<IDefinition<CardTraitData>> Run(IRegister<CardTraitData> service)
        {
            var processList = new List<IDefinition<CardTraitData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadTraits(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private List<CardTraitDefinition> LoadTraits(
            IRegister<CardTraitData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CardTraitDefinition>();
            foreach (var child in pluginConfig.GetSection("traits").GetChildren())
            {
                var data = LoadTraitConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CardTraitDefinition? LoadTraitConfiguration(
            IRegister<CardTraitData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }
            var name = key.GetId("Trait", id);
            var data = new CardTraitData();

            // TraitState
            var traitStateReference = configuration.GetSection("name").ParseReference();
            if (traitStateReference == null)
                return null;

            var traitStateName = traitStateReference.id;
            var modReference = traitStateReference.mod_reference ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !traitStateName.GetFullyQualifiedName<CardTraitState>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                logger.Log(LogLevel.Error, $"Failed to load trait state name {traitStateName} in {name} with mod reference {modReference}, Make sure the class inherits from CardTraitState");
                return null;
            }
            AccessTools
                .Field(typeof(CardTraitData), "traitStateName")
                .SetValue(data, fullyQualifiedName);

            var paramCardType = CardStatistics.CardTypeTarget.Any;
            AccessTools
                .Field(typeof(CardTraitData), "paramCardType")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("card_type", "param_card_type").ParseCardTypeTarget() ?? paramCardType
                );

            var paramEntryDuration = CardStatistics.EntryDuration.ThisTurn;
            AccessTools
                .Field(typeof(CardTraitData), "paramEntryDuration")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("entry_duraction", "param_entry_duration").ParseEntryDuration()
                        ?? paramEntryDuration
                );

            var paramStr = "";
            AccessTools
                .Field(typeof(CardTraitData), "paramStr")
                .SetValue(data, configuration.GetSection("param_str").ParseString() ?? paramStr);

            var paramDescription = configuration.GetSection("param_description").ParseLocalizationTerm();
            if (paramDescription != null)
            {
                var descriptionKey = $"CardTraitData_descriptionKey-{name}";
                paramDescription.Key = descriptionKey;
                localizationService.Register(paramDescription.Key, paramDescription);
                AccessTools
                    .Field(typeof(CardTraitData), "paramDescription")
                    .SetValue(data, descriptionKey);
            }

            var paramInt = 0;
            AccessTools
                .Field(typeof(CardTraitData), "paramInt")
                .SetValue(data, configuration.GetSection("param_int").ParseInt() ?? paramInt);

            var paramInt2 = 0;
            AccessTools
                .Field(typeof(CardTraitData), "paramInt2")
                .SetValue(data, configuration.GetSection("param_int_2").ParseInt() ?? paramInt2);

            var paramInt3 = 0;
            AccessTools
                .Field(typeof(CardTraitData), "paramInt3")
                .SetValue(data, configuration.GetSection("param_int_3").ParseInt() ?? paramInt3);

            var paramFloat = 1f;
            AccessTools
                .Field(typeof(CardTraitData), "paramFloat")
                .SetValue(data, configuration.GetSection("param_float").ParseFloat() ?? paramFloat);

            var paramUseScalingParams = false;
            AccessTools
                .Field(typeof(CardTraitData), "paramUseScalingParams")
                .SetValue(
                    data,
                    configuration.GetSection("param_use_scaling_params").ParseBool()
                        ?? paramUseScalingParams
                );

            var paramBool = false;
            AccessTools
                .Field(typeof(CardTraitData), "paramBool")
                .SetValue(data, configuration.GetSection("param_bool").ParseBool() ?? paramBool);

            var traitIsRemovable = true;
            AccessTools
                .Field(typeof(CardTraitData), "traitIsRemovable")
                .SetValue(
                    data,
                    configuration.GetSection("trait_is_removable").ParseBool() ?? traitIsRemovable
                );

            var tooltipSuppressed = false;
            AccessTools
                .Field(typeof(CardTraitData), "tooltipSuppressed")
                .SetValue(
                    data,
                    configuration.GetSection("tooltip_suppressed").ParseBool() ?? tooltipSuppressed
                );

            var effectTextSuppressed = false;
            AccessTools
                .Field(typeof(CardTraitData), "effectTextSuppressed")
                .SetValue(
                    data,
                    configuration.GetSection("effect_text_suppressed").ParseBool()
                        ?? effectTextSuppressed
                );

            var statusEffectTooltipsSuppressed = false;
            AccessTools
                .Field(typeof(CardTraitData), "statusEffectTooltipsSuppressed")
                .SetValue(
                    data,
                    configuration.GetSection("status_effect_tooltips_suppressed").ParseBool()
                        ?? statusEffectTooltipsSuppressed
                );

            var paramTeamType = Team.Type.None;
            AccessTools
                .Field(typeof(CardTraitData), "paramTeamType")
                .SetValue(
                    data,
                    configuration.GetSection("param_team").ParseTeamType() ?? paramTeamType
                );

            var stackMode = CardTraitData.StackMode.None;
            AccessTools
                .Field(typeof(CardTraitData), "stackMode")
                .SetValue(
                    data,
                    configuration.GetSection("stack_mode").ParseStackMode() ?? stackMode
                );

            var drawInDeploymentPhase = false;
            AccessTools
                .Field(typeof(CardTraitData), "drawInDeploymentPhase")
                .SetValue(
                    data,
                    configuration.GetSection("draw_in_deployment_phase").ParseBool()
                        ?? drawInDeploymentPhase
                );

            service.Register(name, data);
            return new CardTraitDefinition(key, data, configuration) { Id = id };
        }
    }
}
