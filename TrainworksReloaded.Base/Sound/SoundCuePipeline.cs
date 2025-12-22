using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using static ShinyShoe.Audio.CoreSoundEffectData;

namespace TrainworksReloaded.Base.Sound
{
    public class SoundCuePipeline : IDataPipeline<IRegister<SoundCueDefinition>, SoundCueDefinition>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<SoundCuePipeline> logger;
        private readonly GlobalSoundCueDelegator globalSoundDelegator;

        public SoundCuePipeline(PluginAtlas atlas, IModLogger<SoundCuePipeline> logger, GlobalSoundCueDelegator globalSoundDelegator)
        {
            this.atlas = atlas;
            this.logger = logger;
            this.globalSoundDelegator = globalSoundDelegator;
        }

        public List<IDefinition<SoundCueDefinition>> Run(IRegister<SoundCueDefinition> service)
        {
            var processList = new List<IDefinition<SoundCueDefinition>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadItems(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        private IEnumerable<IDefinition<SoundCueDefinition>> LoadItems(IRegister<SoundCueDefinition> service, string key, IConfiguration configuration)
        {
            var processList = new List<SoundCueDataDefinition>();
            foreach (var child in configuration.GetSection("sound_effects").GetChildren())
            {
                var data = LoadConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private SoundCueDataDefinition? LoadConfiguration(IRegister<SoundCueDefinition> service, string key, IConfigurationSection configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            var cue_name = configuration.GetSection("name").ParseString();
            var audio_clips = configuration.GetSection("clips").Exists();
            var global = configuration.GetSection("global").ParseBool() ?? false;
            if (id == null || audio_clips == false)
            {
                logger.Log(LogLevel.Error, $"{configuration.Path} missing one or more required properties (id, clips)");
                return null;
            }
            if (cue_name == null && global == false)
            {
                logger.Log(LogLevel.Error, $"Non-global {configuration.Path} missing required property (name)");
                return null;
            }

            var name = key.GetId(TemplateConstants.SoundCueDefinition, id);            
            var volume_min = configuration.GetSection("volume_min").ParseFloat() ?? 1.0f;
            var volume_max = configuration.GetSection("volume_max").ParseFloat() ?? 1.0f;
            var pitch_min = configuration.GetSection("pitch_min").ParseFloat() ?? 1.0f;
            var pitch_max = configuration.GetSection("pitch_max").ParseFloat() ?? 1.0f;
            var loop = configuration.GetSection("loop").ParseBool() ?? false;
            var tags = configuration.GetSection("tags").GetChildren().Select(x => x.ParseString()).Where(x => x != null).Cast<string>().ToArray();

            var sound_cue = new SoundCueDefinition
            {
                Name = global ? name : cue_name,
                VolumeMin = volume_min,
                VolumeMax = volume_max,
                PitchMin = pitch_min,
                PitchMax = pitch_max,
                Loop = loop,
                Tags = tags
            };

            service.Register(name, sound_cue);

            if (global)
                globalSoundDelegator.Add(sound_cue);

            return new SoundCueDataDefinition(key, sound_cue, configuration)
            {
                Id = id,
            };
        }
    }
}
