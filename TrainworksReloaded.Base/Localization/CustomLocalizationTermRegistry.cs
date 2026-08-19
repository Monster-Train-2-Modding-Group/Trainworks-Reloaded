using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Localization
{
    /// <summary>
    /// Note that this Registry does not store localizations, when localizations are registered they are immediately passed
    /// to I2.Loc and are not stored here to save memory.
    /// </summary>
    public class CustomLocalizationTermRegistry : Dictionary<string, LocalizationTerm>, IRegister<LocalizationTerm>
    {
        private readonly IModLogger<CustomLocalizationTermRegistry> logger;
        private readonly LanguageSourceData source0;
        private readonly LanguageSourceData source1;
        private readonly SourceIndices indices0;
        private readonly SourceIndices indices1;
        private readonly HashSet<string> SupportedLanguages = [
            "English",
            "French",
            "German",
            "Russian",
            "Portuguese (Brazil)",
            "Chinese",
            "Spanish",
            "Chinese (Traditional)",
            "Korean",
            "Japanese"
        ];
        readonly HashSet<string> additionalLanguages;
        private bool RequiresInkReload = false;

        public CustomLocalizationTermRegistry(IModLogger<CustomLocalizationTermRegistry> logger)
        {
            this.logger = logger;
            source0 = LocalizationManager.Sources[0];
            source1 = LocalizationManager.Sources[1];

            additionalLanguages = [.. source0.GetLanguages()];
            additionalLanguages.ExceptWith(SupportedLanguages);

            indices0 = new(source0, additionalLanguages);
            indices1 = new(source1, additionalLanguages);
        }

        public new void Add(string key, LocalizationTerm item)
        {
            string termName = $"Default\\{item.Key}";
            LanguageSourceData source = item.SourceIndex == 0 ? source0 : source1;
            SourceIndices indices = item.SourceIndex == 0 ? indices0 : indices1;
            if (item.SourceIndex == 1)
                RequiresInkReload = true;

            TermData termData = source.AddTerm(termName, eTermType.Text, false);

            SetLanguage(termData, indices.English, item.English);
            SetLanguage(termData, indices.French, item.French);
            SetLanguage(termData, indices.German, item.German);
            SetLanguage(termData, indices.Russian, item.Russian);
            SetLanguage(termData, indices.Portuguese, item.Portuguese);
            SetLanguage(termData, indices.Chinese, item.Chinese);
            SetLanguage(termData, indices.Spanish, item.Spanish);
            SetLanguage(termData, indices.ChineseTraditional, item.ChineseTraditional);
            SetLanguage(termData, indices.Korean, item.Korean);
            SetLanguage(termData, indices.Japanese, item.Japanese);

            foreach (var lang in additionalLanguages)
            {
                SetLanguage(termData, indices.OtherIndices[lang], item.OtherLanguages[lang]);
            }
        }

        /// <summary>
        /// Adds a new Localization.
        /// 
        /// Note if this is called post Trainworks initialization you must manaully call
        /// LocalizationManager.Sources[source_index].UpdateDictionary(true)
        /// 
        /// If doing so, it is advisable to only call that function once all localization terms
        /// you wish to make available are added, calling that line after each term added will result
        /// in poor performance.
        /// </summary>
        /// <param name="key">Localization Key</param>
        /// <param name="item">Localization Term</param>
        public void Register(string key, LocalizationTerm item)
        {
            Add(key, item);
        }

        private sealed class SourceIndices
        {
            public readonly int English;
            public readonly int French;
            public readonly int German;
            public readonly int Russian;
            public readonly int Portuguese;
            public readonly int Chinese;
            public readonly int Spanish;
            public readonly int ChineseTraditional;
            public readonly int Korean;
            public readonly int Japanese;
            public readonly Dictionary<string, int> OtherIndices;

            public SourceIndices(LanguageSourceData source, IEnumerable<string> additionalLanguages)
            {
                English = source.GetLanguageIndex("English [en-US]");
                French = source.GetLanguageIndex("French [fr-FR]");
                German = source.GetLanguageIndex("German [de-DE]");
                Russian = source.GetLanguageIndex("Russian");
                Portuguese = source.GetLanguageIndex("Portuguese (Brazil)");
                Chinese = source.GetLanguageIndex("Chinese");
                Spanish = source.GetLanguageIndex("Spanish");
                ChineseTraditional = source.GetLanguageIndex("Chinese (Traditional)");
                Korean = source.GetLanguageIndex("Korean");
                Japanese = source.GetLanguageIndex("Japanese");

                OtherIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (additionalLanguages != null)
                {
                    foreach (var lang in additionalLanguages)
                    {
                        int idx = source.GetLanguageIndex(lang);
                        if (idx >= 0)
                        {
                            OtherIndices[lang] = idx;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Do not call this function directly, it is called by the framework to apply the changes to localization.
        /// 
        /// </summary>
        public void LoadData()
        {
            source0.UpdateDictionary(true);

            if (RequiresInkReload)
            {
                source1.UpdateDictionary(true);
                RequiresInkReload = false;
            }
        }

        private static void SetLanguage(TermData termData, int index, string? value)
        {
            if (index >= 0 && index < termData.Languages.Length)
            {
                termData.Languages[index] = value ?? string.Empty;
            }
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            throw new NotImplementedException();
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out LocalizationTerm? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            throw new NotImplementedException();
        }
    }
}
