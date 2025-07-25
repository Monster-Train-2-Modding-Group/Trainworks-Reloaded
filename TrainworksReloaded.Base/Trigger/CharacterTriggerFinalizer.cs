using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Trigger
{
    public class CharacterTriggerFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CharacterTriggerFinalizer> logger;
        private readonly IRegister<CardEffectData> effectRegister;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;
        private readonly ICache<IDefinition<CharacterTriggerData>> cache;

        public CharacterTriggerFinalizer(
            IModLogger<CharacterTriggerFinalizer> logger,
            IRegister<CardEffectData> effectRegister,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister,
            ICache<IDefinition<CharacterTriggerData>> cache
        )
        {
            this.logger = logger;
            this.effectRegister = effectRegister;
            this.triggerEnumRegister = triggerEnumRegister;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCharacterTrigger(definition);
            }
            cache.Clear();
        }

        private void FinalizeCharacterTrigger(IDefinition<CharacterTriggerData> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var data = definition.Data;

            logger.Log(LogLevel.Debug,
                $"Finalizing Character Trigger {key.GetId(TemplateConstants.CharacterTrigger, definition.Id)}..."
            );

            //handle trigger
            var trigger = CharacterTriggerData.Trigger.OnDeath;
            var triggerReference = configuration.GetSection("trigger").ParseReference();
            if (triggerReference != null)
            {
                if (
                    triggerEnumRegister.TryLookupId(
                        triggerReference.ToId(key, TemplateConstants.CharacterTriggerEnum),
                        out var triggerFound,
                        out var _
                    )
                )
                {
                    trigger = triggerFound;
                }
            }
            AccessTools
                .Field(typeof(CharacterTriggerData), "trigger")
                .SetValue(data, trigger);

            //handle effects cards
            var effectDatas = new List<CardEffectData>();
            var effectReferences = configuration
                .GetSection("effects")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in effectReferences)
            {
                if (
                    effectRegister.TryLookupId(
                        reference.ToId(key, TemplateConstants.Effect),
                        out var effect,
                        out var _
                    )
                )
                {
                    effectDatas.Add(effect);
                }
            }
            AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(data, effectDatas);
        }
    }
}
