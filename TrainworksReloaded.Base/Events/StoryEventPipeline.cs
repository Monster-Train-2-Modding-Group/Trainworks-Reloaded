using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Events
{
    public class StoryEventPipeline : IDataPipeline<IRegister<StoryEventData>, StoryEventData>
    {
        private readonly PluginAtlas atlas;
        private readonly IGuidProvider guidProvider;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IModLogger<StoryEventPipeline> logger;
        private readonly InkLocalizationHelper inkHelper;
        private readonly Dictionary<string, string> englishToKeyDict;

        public StoryEventPipeline(PluginAtlas atlas,
            IGuidProvider guidProvider,
            IRegister<LocalizationTerm> termRegister,
            IModLogger<StoryEventPipeline> logger)
        {
            this.atlas = atlas;
            this.guidProvider = guidProvider;
            this.termRegister = termRegister;
            this.logger = logger;
            inkHelper = (AccessTools.Field(typeof(LocalizationUtil), "_inkHelper").GetValue(null) as InkLocalizationHelper)!;
            englishToKeyDict = (AccessTools.Field(typeof(InkLocalizationHelper), "_englishToKeyDict").GetValue(inkHelper) as Dictionary<string, string>)!;
        }

        public List<IDefinition<StoryEventData>> Run(IRegister<StoryEventData> service)
        {
            List<IDefinition<StoryEventData>> events = [];
            foreach (var config in atlas.PluginDefinitions)
            {
                events.AddRange(LoadStoryEventDatas(service, config.Key, config.Value.Configuration));
            }
            return events;
        }

        private List<IDefinition<StoryEventData>> LoadStoryEventDatas(IRegister<StoryEventData> service, string key, IConfiguration pluginConfig)
        {
            List<IDefinition<StoryEventData>> events = [];
            foreach (var child in pluginConfig.GetSection("events").GetChildren())
            {
                var evt = LoadConfiguration(service, key, child);
                if (evt != null)
                {
                    events.Add(evt);
                }
            }
            return events;
        }

        private StoryEventDefinition? LoadConfiguration(IRegister<StoryEventData> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            var knot_name = configuration.GetSection("knot_name").ParseString();
            if (id == null || knot_name == null)
            {
                return null;
            }
            var name = key.GetId(TemplateConstants.StoryEvent, id);
            var data = ScriptableObject.CreateInstance<StoryEventData>();
            var guid = guidProvider.GetGuidDeterministic(name).ToString();

            AccessTools.Field(typeof(StoryEventData), "id").SetValue(data, guid);
            data.name = name;
            AccessTools.Field(typeof(StoryEventData), "storyId").SetValue(data, id);
            AccessTools.Field(typeof(StoryEventData), "knotName").SetValue(data, knot_name);

            AccessTools
                .Field(typeof(StoryEventData), "numRunsCompletedToSee")
                .SetValue(data, configuration.GetSection("num_runs_completed_to_see").ParseInt() ?? 1);

            AccessTools
                .Field(typeof(StoryEventData), "priorityTicketCount")
                .SetValue(data, configuration.GetSection("priority_ticket_count").ParseInt() ?? 1);

            AccessTools
                .Field(typeof(StoryEventData), "numClassesNeededToShow")
                .SetValue(data, configuration.GetSection("num_classes_needed_to_show").ParseInt() ?? 1);

            AccessTools
                .Field(typeof(StoryEventData), "covenantLevelRequired")
                .SetValue(data, configuration.GetSection("covenant_level_required").ParseInt() ?? 0);

            AccessTools
                .Field(typeof(StoryEventData), "mainClanLevelRequired")
                .SetValue(data, configuration.GetSection("main_clan_level_required").ParseInt() ?? 1);

            AccessTools
                .Field(typeof(StoryEventData), "alliedClanLevelRequired")
                .SetValue(data, configuration.GetSection("allied_clan_level_required").ParseInt() ?? 1);

            AccessTools
                .Field(typeof(StoryEventData), "minDistanceAllowed")
                .SetValue(data, configuration.GetSection("min_distance_allowed").ParseInt() ?? 0);

            AccessTools
                .Field(typeof(StoryEventData), "maxDistanceAllowed")
                .SetValue(data, configuration.GetSection("max_distance_allowed").ParseInt() ?? 0);

            AccessTools
                .Field(typeof(StoryEventData), "allClassesNeededToShow")
                .SetValue(data, configuration.GetSection("all_classes_needed_to_show").ParseBool() ?? false);

            AccessTools
                .Field(typeof(StoryEventData), "requireDlcModeActive")
                .SetValue(data, configuration.GetSection("require_dlc_mode_active").ParseBool() ?? false);

            AccessTools
                .Field(typeof(StoryEventData), "pinChoiceButtons")
                .SetValue(data, configuration.GetSection("pin_choice_buttons").ParseBool() ?? false);

            AccessTools
                .Field(typeof(StoryEventData), "predetermineRandomRelicsToRemove")
                .SetValue(data, configuration.GetSection("predetermine_random_relics_to_remove").ParseBool() ?? false);

            AccessTools
                .Field(typeof(StoryEventData), "associatedDLC")
                .SetValue(data, configuration.GetSection("required_dlc").ParseDLC() ?? ShinyShoe.DLC.None);

            AccessTools
                .Field(typeof(StoryEventData), "designContributor")
                .SetValue(data, configuration.GetSection("design_contributor").ParseString());

            // Translations for Ink script text goes to langauge source index 1.
            var terms = configuration.GetSection("texts").GetChildren().Select(x => x.ParseLocalizationTerm()).Where(x => x != null).Cast<Localization.LocalizationTerm>();
            int i = 0;
            foreach (var term in terms)
            {
                if (inkHelper.GetInkKey(term.English) != null)
                {
                    logger.Log(LogLevel.Warning, $"Dropping line {term.English}. If this is a line from the vanilla game, it is safe to drop the line from the json definition. " +
                        $"Lines in ink scripts must be unique across all mods and base game.");
                    /// TODO possible merge of localization term if more translations are provided.
                    continue;
                }
                term.SourceIndex = 1;
                term.Key = $"InkLoc-{knot_name}{i}";
                englishToKeyDict.Add(term.English, term.Key);
                termRegister.Register(term.Key, term);
                i++;
            }

            service.Register(name, data);

            return new StoryEventDefinition(key, id, data, configuration);
        }
    }
}