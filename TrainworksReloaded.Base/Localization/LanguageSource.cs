using System;
using System.Collections.Generic;
using System.Text;

namespace TrainworksReloaded.Base.Localization
{
    public class LanguageSource
    {
        private readonly string language;
        private readonly Dictionary<string, string>[] translationSources;
        public string Language => language;
        public Dictionary<string, string>[] TranslationSources => translationSources;

        public LanguageSource(string language, Dictionary<string, string>[] translationSources)
        { 
            this.language = language;
            this.translationSources = translationSources;
        }
    }
}
