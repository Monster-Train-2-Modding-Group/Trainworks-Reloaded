using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TrainworksReloaded.Base.Prefab
{
    public class AssetReferenceGameObjectRegister
        : Dictionary<string, AssetReferenceGameObject>,
            IRegister<AssetReferenceGameObject>
    {
        private readonly IRegister<GameObject> objectRegister;

        public AssetReferenceGameObjectRegister(IRegister<GameObject> objectRegister)
        {
            this.objectRegister = objectRegister;
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return objectRegister.GetAllIdentifiers(identifierType);
        }

        public void Register(string key, AssetReferenceGameObject item)
        {
            //
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out AssetReferenceGameObject? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = false;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    if (objectRegister.TryLookupName(identifier, out GameObject? readableGameObject, out bool? readableModded))
                    {
                        lookup = new AssetReferenceGameObject();
                        IsModded = readableModded;
                        lookup.SetAssetAndId(Hash128.Compute(identifier).ToString(), readableGameObject);
                        return true;
                    }

                    lookup = new AssetReferenceGameObject();
                    lookup.SetId(identifier);
                    return true;
                case RegisterIdentifierType.GUID:
                    if (objectRegister.TryLookupId(identifier, out GameObject? guidGameObject, out bool? guidModded))
                    {
                        lookup = new AssetReferenceGameObject();
                        IsModded = guidModded;
                        lookup.SetAssetAndId(Hash128.Compute(identifier).ToString(), guidGameObject);
                        return true;
                    }

                    lookup = new AssetReferenceGameObject();
                    lookup.SetId(identifier);
                    return true;
                default:
                    return false;
            }
        }
    }
}
