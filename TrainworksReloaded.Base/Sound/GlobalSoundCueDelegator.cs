using System;
using System.Collections.Generic;
using System.Text;
using static ShinyShoe.Audio.CoreSoundEffectData;

namespace TrainworksReloaded.Base.Sound
{
    public class GlobalSoundCueDelegator
    {
        internal List<SoundCueDefinition> GlobalSounds = [];

        public void Add(SoundCueDefinition soundCueDataDefinition)
        {
            GlobalSounds.Add(soundCueDataDefinition);
        }

        public IReadOnlyList<SoundCueDefinition> GetSounds()
        { 
            return GlobalSounds; 
        }
    }
}
