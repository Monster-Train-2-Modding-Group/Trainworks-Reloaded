using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Enum;
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
                FinalizeRelicData((definition as RelicDataDefinition)!);
            }
            cache.Clear();
        }

        private void FinalizeRelicData(RelicDataDefinition definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var copyData = definition.CopyData;
            var key = definition.Key;
            var overrideMode = definition.Override;

            logger.Log(LogLevel.Info, $"Finalizing Relic {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            // Handle relic sprite
            AccessTools.Field(typeof(RelicData), "icon").SetValue(data, copyData.GetIcon());
            var iconConfig = configuration.GetSection("icon");
            var iconReference = iconConfig.ParseReference();
            if (
                iconReference != null
                && spriteRegister.TryLookupId(
                    iconReference.ToId(key, TemplateConstants.Sprite),
                    out var spriteLookup,
                    out var _,
                    iconReference.context
                )
            )
            {
                AccessTools.Field(typeof(RelicData), "icon").SetValue(data, spriteLookup);
            }
            else if (overrideMode == OverrideMode.Replace && iconReference == null && iconConfig.Exists())
            {
                AccessTools.Field(typeof(RelicData), "icon").SetValue(data, null);
            }

            // Handle relic activated sprite
            AccessTools.Field(typeof(RelicData), "iconSmall").SetValue(data, copyData.GetIconSmall());
            var iconSmallConfig = configuration.GetSection("icon_small");
            var iconSmallReference = iconSmallConfig.ParseReference();
            if (
                iconSmallReference != null
                && spriteRegister.TryLookupId(
                    iconSmallReference.ToId(key, TemplateConstants.Sprite),
                    out var activatedSpriteLookup,
                    out var _,
                    iconSmallReference?.context
                )
            )
            {
                AccessTools.Field(typeof(RelicData), "iconSmall").SetValue(data, activatedSpriteLookup);
            }
            else if (overrideMode == OverrideMode.Replace && iconReference == null && iconConfig.Exists())
            {
                AccessTools.Field(typeof(RelicData), "iconSmall").SetValue(data, null);
            }

            //handle relic effects
            var relicEffects = copyData.GetEffects() ?? [];
            if (copyData != data)
                relicEffects = [.. relicEffects];
            IConfigurationSection effectsConfig = configuration.GetSection("relic_effects");
            if (overrideMode == OverrideMode.Replace && effectsConfig.Exists())
            {
                relicEffects.Clear();
            }
            var relicEffectsReferences = effectsConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in relicEffectsReferences)
            {
                if (relicEffectRegister.TryLookupId(reference.ToId(key, TemplateConstants.RelicEffectData), out var relicEffectLookup, out var _, reference.context))
                {
                    relicEffects.Add(relicEffectLookup);
                }
            }
            AccessTools.Field(typeof(RelicData), "effects").SetValue(data, relicEffects);
        }
    }
}