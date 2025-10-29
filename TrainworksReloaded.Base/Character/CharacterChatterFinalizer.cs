using HarmonyLib;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Interfaces;
using static CharacterChatterData;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterChatterFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CharacterChatterFinalizer> logger;
        private readonly ICache<IDefinition<CharacterChatterData>> cache;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;
        private readonly IRegister<CharacterChatterData> chatterRegister;
        private readonly IRegister<LocalizationTerm> termRegister;

        public CharacterChatterFinalizer(
            IModLogger<CharacterChatterFinalizer> logger,
            ICache<IDefinition<CharacterChatterData>> cache,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister,
            IRegister<CharacterChatterData> chatterRegister,
            IRegister<LocalizationTerm> termRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.triggerEnumRegister = triggerEnumRegister;
            this.chatterRegister = chatterRegister;
            this.termRegister = termRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCharacterChatterData(definition);
            }
            cache.Clear();
        }

        public void FinalizeCharacterChatterData(IDefinition<CharacterChatterData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            var name = data.name;

            logger.Log(LogLevel.Info, $"Finalizing Character Chatter {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            int i = 0;
            List<TriggerChatterExpressionData> triggerExpressions = [];
            foreach (var child in configuration.GetSection("trigger_expressions").GetChildren())
            {
                var trigger = CharacterTriggerData.Trigger.OnDeath;
                var triggerReference = configuration.GetSection("trigger").ParseReference();
                if (triggerReference != null)
                {
                    if (
                        triggerEnumRegister.TryLookupId(
                            triggerReference.ToId(key, TemplateConstants.CharacterTriggerEnum),
                            out var triggerFound,
                            out var _,
                            triggerReference.context
                        )
                    )
                    {
                        trigger = triggerFound;
                    }
                }

                var term = child.GetSection("expressions").ParseLocalizationTerm();
                if (term != null)
                {
                    term.Key = $"CharacterChatterData_triggerExpressions{i}-{name}";
                    termRegister.Add(term.Key, term);
                    triggerExpressions.Add(new TriggerChatterExpressionData
                    {
                        trigger = trigger,
                        locKey = term.Key,
                    });
                }
                i++;
            }
            AccessTools.Field(typeof(CharacterChatterData), "characterTriggerExpressions").SetValue(data, triggerExpressions);

            var chatterReference = configuration.GetSection("base_chatter").ParseReference();
            if (chatterReference != null)
            {
                if (chatterRegister.TryLookupId(chatterReference.ToId(key, TemplateConstants.Chatter), out var lookup, out var _, chatterReference.context))
                {
                    AccessTools.Field(typeof(CharacterChatterData), "baseData").SetValue(data, lookup);
                }
            }
        }
    }
}
