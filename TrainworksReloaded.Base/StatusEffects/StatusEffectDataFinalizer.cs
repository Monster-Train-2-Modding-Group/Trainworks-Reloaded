using HarmonyLib;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.StatusEffects
{
    public class StatusEffectDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<StatusEffectDataFinalizer> logger;
        private readonly ICache<IDefinition<StatusEffectData>> cache;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<StatusEffectData.TriggerStage> triggerStageRegister;

        public StatusEffectDataFinalizer(IModLogger<StatusEffectDataFinalizer> logger, ICache<IDefinition<StatusEffectData>> cache, IRegister<VfxAtLoc> vfxRegister, IRegister<Sprite> spriteRegister, IRegister<StatusEffectData.TriggerStage> triggerStageRegister)
        {
            this.logger = logger;
            this.cache = cache;
            this.vfxRegister = vfxRegister;
            this.spriteRegister = spriteRegister;
            this.triggerStageRegister = triggerStageRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeStatusEffect(definition);
            }
            cache.Clear();
        }

        public void FinalizeStatusEffect(IDefinition<StatusEffectData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing StatusEffect {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var icon = configuration.GetSection("icon").ParseReference();
            if (
                icon != null
                && spriteRegister.TryLookupId(
                    icon.ToId(key, TemplateConstants.Sprite),
                    out var lookup,
                    out var _,
                    icon.context
                )
            )
            {
                AccessTools.Field(typeof(StatusEffectData), "icon").SetValue(data, lookup);
            }

            var addedVFX = configuration.GetSection("added_vfx").ParseReference();
            if (vfxRegister.TryLookupId(addedVFX?.ToId(key, TemplateConstants.Vfx) ?? "", out var added_vfx, out var _, addedVFX?.context))
            {
                AccessTools.Field(typeof(StatusEffectData), "addedVFX").SetValue(data, added_vfx);
            }

            var moreAddedVFX = new VfxAtLocList();
            var moreAddedVFXList = moreAddedVFX.GetVfxList();
            var addedVfxReferences = configuration.GetSection("more_added_vfx")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in addedVfxReferences)
            {
                if (vfxRegister.TryLookupId(reference.ToId(key, TemplateConstants.Vfx), out var vfx, out var _, reference.context))
                {
                    moreAddedVFXList.Add(vfx);
                }
            }
            AccessTools.Field(typeof(StatusEffectData), "moreAddedVFX").SetValue(data, moreAddedVFX);

            var persistentVFX = configuration.GetSection("persistent_vfx").ParseReference();
            if (vfxRegister.TryLookupId(persistentVFX?.ToId(key, TemplateConstants.Vfx) ?? "", out var persistent_vfx, out var _, persistentVFX?.context))
            {
                AccessTools.Field(typeof(StatusEffectData), "persistentVFX").SetValue(data, persistent_vfx);
            }

            var morePersistentVFX = new VfxAtLocList();
            var morePersistentVFXList = morePersistentVFX.GetVfxList();
            var persistentVfxReferences = configuration.GetSection("more_persistent_vfx")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in persistentVfxReferences)
            {
                if (vfxRegister.TryLookupId(reference.ToId(key, TemplateConstants.Vfx), out var vfx, out var _, reference.context))
                {
                    morePersistentVFXList.Add(vfx);
                }
            }
            AccessTools.Field(typeof(StatusEffectData), "morePersistentVFX").SetValue(data, morePersistentVFX);

            var triggeredVFX = configuration.GetSection("triggered_vfx").ParseReference();
            if (vfxRegister.TryLookupId(triggeredVFX?.ToId(key, TemplateConstants.Vfx) ?? "", out var triggered_vfx, out var _, triggeredVFX?.context))
            {
                AccessTools.Field(typeof(StatusEffectData), "triggeredVFX").SetValue(data, triggered_vfx);
            }

            var moreTriggeredVFX = new VfxAtLocList();
            var moreTriggeredVFXList = moreTriggeredVFX.GetVfxList();
            var triggeredVfxReferences = configuration.GetSection("more_triggered_vfx")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in triggeredVfxReferences)
            {
                if (vfxRegister.TryLookupId(reference.ToId(key, TemplateConstants.Vfx), out var vfx, out var _, reference.context))
                {
                    moreTriggeredVFXList.Add(vfx);
                }
            }
            AccessTools.Field(typeof(StatusEffectData), "moreTriggeredVFX").SetValue(data, moreTriggeredVFX);

            var removedVFX = configuration.GetSection("removed_vfx").ParseReference();
            if (vfxRegister.TryLookupId(removedVFX?.ToId(key, TemplateConstants.Vfx) ?? "", out var removed_vfx, out var _, removedVFX?.context))
            {
                AccessTools.Field(typeof(StatusEffectData), "removedVFX").SetValue(data, removed_vfx);
            }

            var moreRemovedVFX = new VfxAtLocList();
            var moreRemovedVFXList = moreRemovedVFX.GetVfxList();
            var removedVfxReferences = configuration.GetSection("more_removed_vfx")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in removedVfxReferences)
            {
                if (vfxRegister.TryLookupId(reference.ToId(key, TemplateConstants.Vfx), out var vfx, out var _, reference.context))
                {
                    moreRemovedVFXList.Add(vfx);
                }
            }
            AccessTools.Field(typeof(StatusEffectData), "moreRemovedVFX").SetValue(data, moreRemovedVFX);

            var affectedVFXReference = configuration.GetSection("affected_vfx").ParseReference();
            if (vfxRegister.TryLookupId(affectedVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var affected_vfx, out var _, affectedVFXReference?.context))
            {
                AccessTools.Field(typeof(StatusEffectData), "affectedVFX").SetValue(data, affected_vfx);
            }

            var triggerStage = configuration.GetSection("trigger_stage").ParseReference();
            if (triggerStage != null)
            {
                triggerStageRegister.TryLookupId(
                                    triggerStage.ToId(key, TemplateConstants.StatusEffectTriggerStageEnum),
                                    out var stage, out var _, triggerStage.context);
                AccessTools.Field(typeof(StatusEffectData), "triggerStage").SetValue(data, stage);
            }

            var triggerStageList = data.GetAdditionalTriggerStages();
            var additionalTriggerStages = configuration
                .GetSection("additional_trigger_stages")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var additionalTriggerStage in additionalTriggerStages)
            {
                triggerStageRegister.TryLookupId(
                                    additionalTriggerStage.ToId(key, TemplateConstants.StatusEffectTriggerStageEnum),
                                    out var stage, out var _, additionalTriggerStage.context);
                triggerStageList.Add(stage);
            }
        }
    }
}

