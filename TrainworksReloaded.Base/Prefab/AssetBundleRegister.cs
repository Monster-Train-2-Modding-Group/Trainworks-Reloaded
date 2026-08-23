using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class AssetBundleRegister : Dictionary<string, AssetBundle>, IRegister<AssetBundle>
    {
        private readonly IModLogger<AssetBundleRegister> logger;

        public AssetBundleRegister(IModLogger<AssetBundleRegister> logger)
        {
            this.logger = logger;
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. Keys];
        }

        public void Register(string key, AssetBundle item)
        {
            logger.Log(LogLevel.Debug, $"Register AssetBundle ({key})");
            this.Add(key, item);
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out AssetBundle? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }
    }
}
