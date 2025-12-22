using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;
using static ShinyShoe.Audio.CoreSoundEffectData;

namespace TrainworksReloaded.Base.Sound
{
    public class SoundCueDataDefinition(string key, SoundCueDefinition data, IConfiguration configuration)
        : IDefinition<SoundCueDefinition>
    {
        public string Key { get; set; } = key;
        public SoundCueDefinition Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
