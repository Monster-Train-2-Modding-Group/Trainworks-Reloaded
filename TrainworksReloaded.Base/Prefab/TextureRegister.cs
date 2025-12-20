using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class TextureRegister(IModLogger<TextureRegister> logger) : Dictionary<string, Texture2D>, IRegister<Texture2D>
    {
        private readonly IModLogger<TextureRegister> logger = logger;

        public void Register(string key, Texture2D item)
        {
            logger.Log(LogLevel.Debug, $"Register Texture {key}");
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out Texture2D? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var icon in this.Values)
                    {
                        if (icon.name == identifier)
                        {
                            lookup = icon;
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }

    }
}
