using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Localization
{
    public class CustomLocalizationTermRegistry
        : Dictionary<string, LocalizationTerm>,
            IRegister<LocalizationTerm>
    {
        private readonly IModLogger<CustomLocalizationTermRegistry> logger;

        public CustomLocalizationTermRegistry(IModLogger<CustomLocalizationTermRegistry> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, LocalizationTerm item)
        {
            this.Add(key, item);
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

            public SourceIndices(LanguageSourceData source, List<string> additionalLanguages)
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

        public void LoadData(List<string> additionalLanguages)
        {
            logger.Log(LogLevel.Error, "START");
            LanguageSourceData source0 = LocalizationManager.Sources[0];
            LanguageSourceData source1 = LocalizationManager.Sources[1];

            SourceIndices indices0 = new(source0, additionalLanguages);
            SourceIndices indices1 = new(source1, additionalLanguages);

            foreach (var term in this.Values)
            {
                string termName = $"Default\\{term.Key}";
                LanguageSourceData source = term.SourceIndex == 0 ? source0 : source1;
                SourceIndices indices = term.SourceIndex == 0 ? indices0 : indices1;

                TermData termData = source.AddTerm(termName, eTermType.Text, false);

                SetLanguage(termData, indices.English, term.English);
                SetLanguage(termData, indices.French, term.French);
                SetLanguage(termData, indices.German, term.German);
                SetLanguage(termData, indices.Russian, term.Russian);
                SetLanguage(termData, indices.Portuguese, term.Portuguese);
                SetLanguage(termData, indices.Chinese, term.Chinese);
                SetLanguage(termData, indices.Spanish, term.Spanish);
                SetLanguage(termData, indices.ChineseTraditional, term.ChineseTraditional);
                SetLanguage(termData, indices.Korean, term.Korean);
                SetLanguage(termData, indices.Japanese, term.Japanese);

                foreach (var lang in additionalLanguages)
                {
                    SetLanguage(termData, indices.OtherIndices[lang], term.OtherLanguages[lang]);
                }
            }

            source0.UpdateDictionary(true);
            source1.UpdateDictionary(true);

            AccessTools.Field(typeof(LocalizationUtil), "_inkHelper").SetValue(null, null);
            // Neccessary to pull in the ink keys to the dictionary.
            LocalizationUtil.InitInkHelper();
            logger.Log(LogLevel.Error, "END");
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
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => []
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out LocalizationTerm? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }
    }
}
