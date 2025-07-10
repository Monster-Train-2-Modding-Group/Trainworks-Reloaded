using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using I2.Loc;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Localization
{
    public class LanguageSourceRegister : Dictionary<string, LanguageSource>, IRegister<LanguageSource>
    {
        private readonly IModLogger<LanguageSourceRegister> logger;

        public LanguageSourceRegister(IModLogger<LanguageSourceRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, LanguageSource item)
        {
            this.Add(key, item);
        }

        public void LoadData()
        {
            if (this.Count == 0)
                return;
            LoadTranslations(0);
            LoadTranslations(1);
        }

        private void LoadTranslations(int source_index) 
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                $"Key,Type,Desc,Group,Descriptions,{String.Join(',', this.Keys)}"
            );

            List<Dictionary<string, string>> sources = this.Values.Select(x => x.TranslationSources[source_index]).ToList();

            HashSet<string> keys = [];
            foreach (var source in sources)
            {
                keys.UnionWith(source.Keys);
            }

            foreach (var key in keys)
            {
                builder.Append($"{key},Text,,,");
                foreach (var source in sources)
                {
                    builder.Append($",\"{source.GetValueOrDefault(key, string.Empty)}\"");

                }
                builder.AppendLine();
            }

            List<string> categories = LocalizationManager.Sources[source_index].GetCategories(true);
            foreach (string Category in categories)
            {
                LocalizationManager
                    .Sources[source_index]
                    .Import_CSV(Category, builder.ToString(), eSpreadsheetUpdateMode.Merge, ',');
            }
            LocalizationManager.LocalizeAll(true);
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out LanguageSource? lookup, [NotNullWhen(true)] out bool? IsModded)
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
