using HarmonyLib;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine.AddressableAssets;

namespace TrainworksReloaded.Base.Prefab
{
    public class VfxFinalizer : IDataFinalizer
    {
        private readonly ICache<IDefinition<VfxAtLoc>> cache;
        private readonly IRegister<AssetReferenceGameObject> assetReferenceRegister;

        public VfxFinalizer(
            IRegister<AssetReferenceGameObject> assetReferenceRegister,
            ICache<IDefinition<VfxAtLoc>> cache
        )
        {
            this.assetReferenceRegister = assetReferenceRegister;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeVfxAtLoc(definition);
            }
            cache.Clear();
        }

        private void FinalizeVfxAtLoc(IDefinition<VfxAtLoc> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var data = definition.Data;

            var vfxLeft = configuration.GetSection("vfx_left").ParseReference();
            if (
                vfxLeft != null
                && assetReferenceRegister.TryLookupId(
                    vfxLeft.ToId(key,  TemplateConstants.GameObject),
                    out var vfxLeftData,
                    out var _,
                    vfxLeft.context
                )
            )
            {
                AccessTools.Field(typeof(VfxAtLoc), "vfxPrefabRefLeft").SetValue(data, vfxLeftData);
            }

            var vfxRight = configuration.GetSection("vfx_left").ParseReference();
            if (
                vfxRight != null
                && assetReferenceRegister.TryLookupId(
                    vfxRight.ToId(key, TemplateConstants.GameObject),
                    out var vfxRightData,
                    out var _,
                    vfxRight.context
                )
            )
            {
                AccessTools
                    .Field(typeof(VfxAtLoc), "vfxPrefabRefRight")
                    .SetValue(data, vfxRightData);
            }
        }
    }
}
