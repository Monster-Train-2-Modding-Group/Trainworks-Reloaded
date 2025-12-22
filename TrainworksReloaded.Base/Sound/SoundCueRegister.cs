using HarmonyLib;
using ShinyShoe.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using static ShinyShoe.Audio.CoreSoundEffectData;

namespace TrainworksReloaded.Base.Sound
{
    public class SoundCueRegister : Dictionary<string, SoundCueDefinition>, IRegister<SoundCueDefinition>
    {
        private readonly IModLogger<SoundCueRegister> logger;
        private readonly Lazy<CoreSoundEffectData?> soundEffectData;

        public SoundCueRegister(IModLogger<SoundCueRegister> logger, GameDataClient client)
        {
            this.logger = logger;
            soundEffectData = new(() => {
                if (client.TryGetProvider<SoundManager>(out SoundManager? provider))
                {
                    var audioSystem = AccessTools.Field(typeof(SoundManager), "audioSystem").GetValue(provider) as CoreAudioSystem;
                    var coreAudioSystemData = AccessTools.Field(typeof(CoreAudioSystem), "AudioSystemData").GetValue(audioSystem) as CoreAudioSystemData;
                    return coreAudioSystemData?.GlobalSoundEffectData;
                }
                return null;
            });
        }

        public void Register(string key, SoundCueDefinition item)
        {
            logger.Log(LogLevel.Info, $"Register SoundCueDefinition ({key})");
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out SoundCueDefinition? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        return true;
                    }
                    if (soundEffectData.Value == null) return false;
                    foreach (var sound_cue in soundEffectData.Value.Sounds)
                    {
                        if (sound_cue.Name == identifier)
                        {
                            lookup = sound_cue;
                            IsModded = false;
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        return true;
                    }
                    if (soundEffectData.Value == null) return false;
                    foreach (var sound_cue in soundEffectData.Value.Sounds)
                    {
                        if (sound_cue.Name == identifier)
                        {
                            lookup = sound_cue;
                            IsModded = false;
                            return true;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        public void RegisterGlobalSoundEffects(IReadOnlyList<SoundCueDefinition> sounds)
        {
            var coreEffectSoundData = soundEffectData.Value;
            if (sounds.IsNullOrEmpty() || coreEffectSoundData == null) return;
            var coreSounds = coreEffectSoundData.Sounds.ToList();
            coreSounds.AddRange(sounds);
            coreEffectSoundData.Sounds = coreSounds.ToArray();
        }
    }
}
