using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<RelicDataFinalizer> logger;
        private readonly ICache<IDefinition<RelicData>> cache;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<RelicEffectData> relicEffectRegister;
        public RelicDataFinalizer(
            IModLogger<RelicDataFinalizer> logger,
            ICache<IDefinition<RelicData>> cache,
            IRegister<Sprite> spriteRegister,
            IRegister<RelicEffectData> relicEffectRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
            this.relicEffectRegister = relicEffectRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRelicData(definition);
            }
            cache.Clear();
        }

        private void FinalizeRelicData(IDefinition<RelicData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Debug, $"Finalizing Relic {data.name}...");

            // Handle relic sprite
            var iconSprite = configuration.GetSection("icon").ParseReference();
            if (
                iconSprite != null
                && spriteRegister.TryLookupId(
                    iconSprite.ToId(key, TemplateConstants.Sprite),
                    out var spriteLookup,
                    out var _
                )
            )
            {
                AccessTools.Field(typeof(RelicData), "icon").SetValue(data, spriteLookup);
            }

            // Handle relic activated sprite
            var iconSmallSprite = configuration.GetSection("icon_small").ParseReference();
            if (
                iconSmallSprite != null
                && spriteRegister.TryLookupId(
                    iconSmallSprite.ToId(key, TemplateConstants.Sprite),
                    out var activatedSpriteLookup,
                    out var _
                )
            )
            {
                AccessTools.Field(typeof(RelicData), "iconSmall").SetValue(data, activatedSpriteLookup);
            }

            //handle relic effects
            var relicEffects = new List<RelicEffectData>();
            var relicEffectsReferences = configuration.GetSection("relic_effects")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in relicEffectsReferences)
            {
                if (relicEffectRegister.TryLookupId(reference.ToId(key, TemplateConstants.RelicEffectData), out var relicEffectLookup, out var _))
                {
                    relicEffects.Add(relicEffectLookup);
                }
            }
            AccessTools.Field(typeof(RelicData), "effects").SetValue(data, relicEffects);
        }
    }
}