using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class TextureRegister : Dictionary<string, Texture2D>, IRegister<Texture2D>
    {
        private readonly IModLogger<TextureRegister> logger;
        private readonly IRegister<Sprite> spriteRegister;

        public TextureRegister(IModLogger<TextureRegister> logger, IRegister<Sprite> spriteRegister)
        {
            this.logger = logger;
            this.spriteRegister = spriteRegister;
        }

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
            if (spriteRegister.TryLookupIdentifier(identifier, identifierType, out var sprite, out IsModded))
            {
                lookup = sprite.texture;
                return true;
            }
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
                    break;
                case RegisterIdentifierType.GUID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        return true;
                    }
                    break;
            }
            IsModded = false;
            return false;
        }
    }
}
