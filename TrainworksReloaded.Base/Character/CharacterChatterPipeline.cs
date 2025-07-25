using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static CharacterChatterData;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterChatterPipeline : IDataPipeline<IRegister<CharacterChatterData>, CharacterChatterData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<CharacterChatterPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;

        public CharacterChatterPipeline(
            PluginAtlas atlas,
            IModLogger<CharacterChatterPipeline> logger,
            IRegister<LocalizationTerm> termRegister
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
        }

        public List<IDefinition<CharacterChatterData>> Run(IRegister<CharacterChatterData> service)
        {
            var processList = new List<IDefinition<CharacterChatterData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(
                    LoadChatter(service, config.Key, config.Value.Configuration)
                );
            }
            return processList;
        }

        /// <summary>
        /// Loads the Card Definitions in
        /// </summary>
        /// <param name="service"></param>
        /// <param name="key"></param>
        /// <param name="pluginConfig"></param>
        /// <returns></returns>
        private List<CharacterChatterDefinition> LoadChatter(
            IRegister<CharacterChatterData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CharacterChatterDefinition>();
            foreach (var child in pluginConfig.GetSection("chatter").GetChildren())
            {
                var data = LoadChatterConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CharacterChatterDefinition? LoadChatterConfiguration(
            IRegister<CharacterChatterData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            CharacterChatterData data = ScriptableObject.CreateInstance<CharacterChatterData>();
            var name = key.GetId(TemplateConstants.Chatter, id);
            data.name = name;

            int i = 0;
            List<ChatterExpressionData> addedExpressions = [];
            foreach (var child in configuration.GetSection("added_expressions").GetChildren())
            {
                var term = child.ParseLocalizationTerm();
                if (term != null)
                {
                    term.Key = $"CharacterChatterData_addedExpressions{i}-{name}";
                    termRegister.Add(term.Key, term);
                    addedExpressions.Add(new ChatterExpressionData
                    {
                        locKey = term.Key,
                    });
                }
                i++;
            }
            AccessTools.Field(typeof(CharacterChatterData), "characterAddedExpressions").SetValue(data, addedExpressions);

            i = 0;
            List<ChatterExpressionData> attackingExpressions = [];
            foreach (var child in configuration.GetSection("attacking_expressions").GetChildren())
            {
                var term = child.ParseLocalizationTerm();
                if (term != null)
                {
                    term.Key = $"CharacterChatterData_attackingExpressions{i}-{name}";
                    termRegister.Add(term.Key, term);
                    attackingExpressions.Add(new ChatterExpressionData
                    {
                        locKey = term.Key,
                    });
                }
                i++;
            }
            AccessTools.Field(typeof(CharacterChatterData), "characterAttackingExpressions").SetValue(data, attackingExpressions);

            i = 0;
            List<ChatterExpressionData> idleExpressions = [];
            foreach (var child in configuration.GetSection("idle_expressions").GetChildren())
            {
                var term = child.ParseLocalizationTerm();
                if (term != null)
                {
                    term.Key = $"CharacterChatterData_idleExpressions{i}-{name}";
                    termRegister.Add(term.Key, term);
                    idleExpressions.Add(new ChatterExpressionData
                    {
                        locKey = term.Key,
                    });
                }
                i++;
            }
            AccessTools.Field(typeof(CharacterChatterData), "characterIdleExpressions").SetValue(data, idleExpressions);

            i = 0;
            List<ChatterExpressionData> slayedExpressions = [];
            foreach (var child in configuration.GetSection("slayed_expressions").GetChildren())
            {
                var term = child.ParseLocalizationTerm();
                if (term != null)
                {
                    term.Key = $"CharacterChatterData_slayedExpressions{i}-{name}";
                    termRegister.Add(term.Key, term);
                    slayedExpressions.Add(new ChatterExpressionData
                    {
                        locKey = term.Key,
                    });
                }
                i++;
            }
            AccessTools.Field(typeof(CharacterChatterData), "characterSlayedExpressions").SetValue(data, slayedExpressions);

            Gender gender = configuration.GetSection("gender").ParseGender(Gender.Neutral);
            AccessTools.Field(typeof(CharacterChatterData), "gender").SetValue(data, gender);

            service.Register(name, data);

            return new CharacterChatterDefinition(key, data, configuration)
            {
                Id = id,
            };
        }
    }
}
