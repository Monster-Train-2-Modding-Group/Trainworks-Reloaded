using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine.AddressableAssets;

namespace TrainworksReloaded.Base.Prefab
{
    public class VfxPipeline : IDataPipeline<IRegister<VfxAtLoc>, VfxAtLoc>
    {
        private readonly PluginAtlas atlas;

        public VfxPipeline(PluginAtlas atlas)
        {
            this.atlas = atlas;
        }

        public List<IDefinition<VfxAtLoc>> Run(IRegister<VfxAtLoc> service)
        {
            var definitions = new List<IDefinition<VfxAtLoc>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                var key = config.Key;
                foreach (
                    var vfxConfig in config.Value.Configuration.GetSection("vfxs").GetChildren()
                )
                {
                    var id = vfxConfig.GetSection("id").Value;
                    if (id == null)
                    {
                        continue;
                    }
                    var name = key.GetId(TemplateConstants.Vfx, id);

                    var vfx = (VfxAtLoc)FormatterServices.GetUninitializedObject(typeof(VfxAtLoc));
                    AccessTools.Field(typeof(VfxAtLoc), "vfxPrefabLeft").SetValue(vfx, null);
                    AccessTools
                        .Field(typeof(VfxAtLoc), "vfxPrefabRefLeft")
                        .SetValue(vfx, new AssetReferenceGameObject());
                    AccessTools.Field(typeof(VfxAtLoc), "vfxPrefabRight").SetValue(vfx, null);
                    AccessTools
                        .Field(typeof(VfxAtLoc), "vfxPrefabRefRight")
                        .SetValue(vfx, new AssetReferenceGameObject());
                    AccessTools
                        .Field(typeof(VfxAtLoc), "spawnLocation")
                        .SetValue(
                            vfx,
                            vfxConfig.GetSection("spawn_location").ParseLocation()
                                ?? VfxAtLoc.Location.None
                        );
                    AccessTools
                        .Field(typeof(VfxAtLoc), "facing")
                        .SetValue(
                            vfx,
                            vfxConfig.GetSection("spawn_location").ParseFacing()
                                ?? VfxAtLoc.Facing.None
                        );

                    service.Register(name, vfx);
                    var definition = new VfxDefinition(key, vfx, vfxConfig)
                    {
                        Id = id,
                        IsModded = true,
                    };
                    definitions.Add(definition);
                }
            }
            return definitions;
        }
    }
}
