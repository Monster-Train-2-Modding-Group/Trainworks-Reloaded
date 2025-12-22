using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static ShinyShoe.Audio.CoreSoundEffectData;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Sound
{
    public class SoundCueFinalizer : IDataFinalizer
    {
        private readonly IRegister<AudioClip> audioClipRegister;
        private readonly IModLogger<SoundCueFinalizer> logger;
        private readonly ICache<IDefinition<SoundCueDefinition>> cache;

        public SoundCueFinalizer(IRegister<AudioClip> audioClipRegister, IModLogger<SoundCueFinalizer> logger, ICache<IDefinition<SoundCueDefinition>> cache)
        {
            this.audioClipRegister = audioClipRegister;
            this.logger = logger;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeItem(definition);
            }
            cache.Clear();
        }

        private void FinalizeItem(IDefinition<SoundCueDefinition> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing Sound Effect {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var references = configuration.GetSection("clips").GetChildren().Select(x => x.ParseReference()).Where(x => x != null).Cast<ReferencedObject>();
            List<AudioClip> clips = [];
            foreach (var reference in references)
            {
                if (audioClipRegister.TryLookupId(reference.ToId(key, TemplateConstants.AudioClip), out var clip, out var _, reference.context))
                {
                    clips.Add(clip);
                }
            }

            data.Clips = clips.ToArray();
        }
    }
}
