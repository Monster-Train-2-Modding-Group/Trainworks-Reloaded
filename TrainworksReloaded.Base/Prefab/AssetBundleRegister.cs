using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace TrainworksReloaded.Base.Prefab
{
    public class AssetBundleRegister : Dictionary<string, AssetBundle>, IRegister<AssetBundle>
    {
        private readonly IModLogger<AssetBundleRegister> logger;

        public AssetBundleRegister(IModLogger<AssetBundleRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, AssetBundle item)
        {
            logger.Log(LogLevel.Info, $"Register AssetBundle ({key})");
            this.Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Values.Select(icon => icon.name)],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out AssetBundle? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            return this.TryGetValue(identifier, out lookup);

        }
    }
}
