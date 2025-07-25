using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine.AddressableAssets;

namespace TrainworksReloaded.Base.Prefab
{
    public class VfxRegister : Dictionary<string, VfxAtLoc>, IRegister<VfxAtLoc>
    {
        public static VfxAtLoc Default { get; set; }

        static VfxRegister()
        {
            Default = (VfxAtLoc)FormatterServices.GetUninitializedObject(typeof(VfxAtLoc));
            AccessTools.Field(typeof(VfxAtLoc), "vfxPrefabLeft").SetValue(Default, null);
            AccessTools
                .Field(typeof(VfxAtLoc), "vfxPrefabRefLeft")
                .SetValue(Default, new AssetReferenceGameObject());
            AccessTools.Field(typeof(VfxAtLoc), "vfxPrefabRight").SetValue(Default, null);
            AccessTools
                .Field(typeof(VfxAtLoc), "vfxPrefabRefRight")
                .SetValue(Default, new AssetReferenceGameObject());
            AccessTools
                .Field(typeof(VfxAtLoc), "spawnLocation")
                .SetValue(Default, VfxAtLoc.Location.None);
            AccessTools.Field(typeof(VfxAtLoc), "facing").SetValue(Default, VfxAtLoc.Facing.None);
        }

        private readonly IModLogger<VfxRegister> logger;

        public VfxRegister(IModLogger<VfxRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, VfxAtLoc item)
        {
            logger.Log(LogLevel.Info, $"Register VFX {key}");
            this.Add(key, item);
        }


        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out VfxAtLoc? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    var result = this.TryGetValue(identifier, out lookup);
                    if (result == false)
                    {
                        lookup = Default;
                        result = true;
                    }
                    return result;
                case RegisterIdentifierType.GUID:
                    var result2 = this.TryGetValue(identifier, out lookup);
                    if (result2 == false)
                    {
                        lookup = Default;
                        result2 = true;
                    }
                    return result2;
                default:
                    return false;
            }
        }
    }
}
