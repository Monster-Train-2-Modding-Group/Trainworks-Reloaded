using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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

        public void LoadData(List<string> additionalLanguages)
        {
            var builder = new StringBuilder();
            var builderInk = new StringBuilder();
            var languagesString = "";
            if (additionalLanguages.Count > 0)
            {
                languagesString = $",{String.Join(',', additionalLanguages)}";
            }
            builder.AppendLine(
                $"Key,Type,Desc,Group,Descriptions,English [en-US],French [fr-FR],German [de-DE],Russian,Portuguese (Brazil),Chinese,Spanish,Chinese (Traditional),Korean,Japanese{languagesString}"
            );
            builderInk.AppendLine(
                $"Key,Type,Desc,Group,Descriptions,English [en-US],French [fr-FR],German [de-DE],Russian,Portuguese (Brazil),Chinese,Spanish,Chinese (Traditional),Korean,Japanese{languagesString}"
            );

            foreach (var term in this.Values)
            {
                logger.Log(LogLevel.Debug, $"Adding Term ({term.Key}) -- ({term.English})");
                var otherLanguages = "";
                if (additionalLanguages.Count > 0)
                {
                    otherLanguages = $",{String.Join(',', additionalLanguages.Select(x => term.OtherLanguages.GetValueOrDefault(x, term.English)))}";
                }
                var builderToUse = (term.SourceIndex == 0) ? builder : builderInk;
                builderToUse.AppendLine(
                    $"{term.Key},{term.Type},{term.Desc},{term.Group},{term.Descriptions},{term.English},{term.French},{term.German},{term.Russian},{term.Portuguese},{term.Chinese},{term.Spanish},{term.ChineseTraditional},{term.Korean},{term.Japanese}{otherLanguages}"
                );
            }

            LocalizationManager.InitializeIfNeeded();
            List<string> categories = LocalizationManager.Sources[0].GetCategories(true);
            foreach (string Category in categories)
            {
                LocalizationManager
                    .Sources[0]
                    .Import_CSV(Category, builder.ToString(), eSpreadsheetUpdateMode.Merge, ',');
                LocalizationManager
                    .Sources[0]
                    .Import_CSV(
                        Category,
                        builder.ToString(),
                        eSpreadsheetUpdateMode.AddNewTerms,
                        ','
                    );
            }

            categories = LocalizationManager.Sources[1].GetCategories(true);
            foreach (string Category in categories)
            {
                LocalizationManager
                    .Sources[1]
                    .Import_CSV(Category, builderInk.ToString(), eSpreadsheetUpdateMode.Merge, ',');
                LocalizationManager
                    .Sources[1]
                    .Import_CSV(
                        Category,
                        builderInk.ToString(),
                        eSpreadsheetUpdateMode.AddNewTerms,
                        ','
                    );
            }
            AccessTools.Field(typeof(LocalizationUtil), "_inkHelper").SetValue(null, null);
            // Neccessary to pull in the ink keys to the dictionary.
            LocalizationUtil.InitInkHelper();
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
