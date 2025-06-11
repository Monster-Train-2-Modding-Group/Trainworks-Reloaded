using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectConditionFinalizer : IDataFinalizer
    {
        private readonly IModLogger<RelicEffectConditionFinalizer> logger;
        private readonly ICache<IDefinition<RelicEffectCondition>> cache;
        private readonly IRegister<SubtypeData> subtypeRegister;

        public RelicEffectConditionFinalizer(
            IModLogger<RelicEffectConditionFinalizer> logger,
            ICache<IDefinition<RelicEffectCondition>> cache,
            IRegister<SubtypeData> register
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.subtypeRegister = register;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeItem(definition);
            }
            cache.Clear();
        }

        private void FinalizeItem(IDefinition<RelicEffectCondition> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Debug, $"Finalizing RelicEffectCondition {key} {definition.Id}...");
            
            var subtypeReference = configuration.GetSection("param_subtype").ParseReference();
            if (subtypeReference != null)
            {
                var id = subtypeReference.ToId(key, TemplateConstants.Subtype);
                subtypeRegister.TryLookupId(id, out var lookup, out var _);
                AccessTools.Field(typeof(RelicEffectCondition), "paramSubtype").SetValue(data, lookup?.Key);
            }
        }
    }
} 