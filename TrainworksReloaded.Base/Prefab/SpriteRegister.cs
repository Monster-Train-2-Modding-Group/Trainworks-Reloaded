using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class SpriteRegister : Dictionary<string, Sprite>, IRegister<Sprite>
    {
        private readonly IModLogger<SpriteRegister> logger;

        public SpriteRegister(IModLogger<SpriteRegister> logger)
        {
            this.logger = logger;
        }


        public void Register(string key, Sprite item)
        {
            logger.Log(LogLevel.Debug, $"Register Sprite ({key})");
            this.Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Values.Select(sprite => sprite.name)],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out Sprite? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var sprite in this.Values)
                    {
                        if (sprite.name == identifier)
                        {
                            lookup = sprite;
                            IsModded = true;
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
