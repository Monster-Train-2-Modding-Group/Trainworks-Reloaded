using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Sound
{
    public class AudioClipRegister : Dictionary<string, AudioClip>, IRegister<AudioClip>
    {
        private readonly IModLogger<AudioClipRegister> logger;

        public AudioClipRegister(IModLogger<AudioClipRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, AudioClip item)
        {
            logger.Log(LogLevel.Info, $"Register AudioClip ({key})");
            this.Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Values.Select(clip => clip.name)],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out AudioClip? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var AudioClip in this.Values)
                    {
                        if (AudioClip.name == identifier)
                        {
                            lookup = AudioClip;
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
