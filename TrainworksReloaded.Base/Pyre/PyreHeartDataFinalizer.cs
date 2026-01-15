using HarmonyLib;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Pyre
{
    public class PyreHeartDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<PyreHeartDataFinalizer> logger;
        private readonly ICache<IDefinition<PyreHeartData>> cache;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<GameObject> gameObjectRegister;
        private readonly IRegister<Sprite> spriteRegister;

        public PyreHeartDataFinalizer(
            IModLogger<PyreHeartDataFinalizer> logger,
            ICache<IDefinition<PyreHeartData>> cache,
            IRegister<GameObject> gameObjectRegister,
            IRegister<RelicData> relicRegister,
            IRegister<Sprite> spriteRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.gameObjectRegister = gameObjectRegister;
            this.relicRegister = relicRegister;
            this.spriteRegister = spriteRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeItem(definition);
            }
            cache.Clear();
        }

        private void FinalizeItem(IDefinition<PyreHeartData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing Pyre Heart {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var artifactReference = configuration.GetSection("pyre_artifact").ParseReference();
            if (artifactReference != null && relicRegister.TryLookupName(artifactReference.ToId(key, TemplateConstants.RelicData), out var lookup, out var _, artifactReference.context))
            {
                if (lookup is PyreArtifactData)
                {
                    AccessTools.Field(typeof(PyreHeartData), "pyreArtifact").SetValue(data, lookup);
                }
                else
                {
                    logger.Log(LogLevel.Warning, $"PyreHeartData {definition.Id} Attempted to add a non-PyreArtifactData RelicData {lookup.name}. Ignoring...");
                }
            }

            var iconReference = configuration.GetSection("icon").ParseReference();
            if (iconReference != null && spriteRegister.TryLookupName(iconReference.ToId(key, TemplateConstants.Sprite), out var iconSprite, out var _, iconReference.context))
            {
                AccessTools.Field(typeof(PyreHeartData), "icon").SetValue(data, iconSprite);
            }

            var iconWinReference = configuration.GetSection("icon_win").ParseReference();
            if (iconWinReference != null && spriteRegister.TryLookupName(iconWinReference.ToId(key, TemplateConstants.Sprite), out var iconWinSprite, out var _, iconWinReference.context))
            {
                AccessTools.Field(typeof(PyreHeartData), "iconWin").SetValue(data, iconWinSprite);
            }

            var iconBossWinReference = configuration.GetSection("icon_boss_win").ParseReference();
            if (iconBossWinReference != null && spriteRegister.TryLookupName(iconBossWinReference.ToId(key, TemplateConstants.Sprite), out var iconBossWinSprite, out var _, iconBossWinReference.context))
            {
                AccessTools.Field(typeof(PyreHeartData), "iconBossWin").SetValue(data, iconBossWinSprite);
            }

            var iconDefeatReference = configuration.GetSection("icon_defeat").ParseReference();
            if (iconDefeatReference != null && spriteRegister.TryLookupName(iconDefeatReference.ToId(key, TemplateConstants.Sprite), out var iconDefeatSprite, out var _, iconDefeatReference.context))
            {
                AccessTools.Field(typeof(PyreHeartData), "iconDefeat").SetValue(data, iconDefeatSprite);
            }

            var vfxPopupReference = configuration.GetSection("vfx_activated_popup").ParseReference();
            if (vfxPopupReference != null && gameObjectRegister.TryLookupName(vfxPopupReference.ToId(key, TemplateConstants.GameObject), out var vfxPopup, out var _, vfxPopupReference.context))
            {
                AccessTools.Field(typeof(PyreHeartData), "vfxActivatedPopupPrefab").SetValue(data, vfxPopup);
            }

            var vfxHudReference = configuration.GetSection("vfx_activated_hud").ParseReference();
            if (vfxHudReference != null && gameObjectRegister.TryLookupName(vfxHudReference.ToId(key, TemplateConstants.GameObject), out var vfxHud, out var _, vfxHudReference.context))
            {
                AccessTools.Field(typeof(PyreHeartData), "vfxActivatedHudPrefab").SetValue(data, vfxHud);
            }
        }
    }
}
