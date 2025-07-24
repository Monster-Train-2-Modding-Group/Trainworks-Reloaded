using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Localization
{
    public class ReplacementStringRegistry
         : Dictionary<string, ReplacementStringData>,
             IRegister<ReplacementStringData>
    {
        private readonly IModLogger<CustomLocalizationTermRegistry> logger;
        private readonly GameDataClient gameDataClient;

        public ReplacementStringRegistry(IModLogger<CustomLocalizationTermRegistry> logger, GameDataClient gameDataClient)
        {
            this.logger = logger;
            this.gameDataClient = gameDataClient;
        }

        public void Register(string key, ReplacementStringData item)
        {
            this.Add(key, item);
        }

        public void LoadData()
        {
            if (!gameDataClient.TryGetProvider<LanguageManager>(out var manager))
            {
                logger.Log(LogLevel.Error, "Unable to get replacement strings dictionary. LangaugeManager not available.");
                return;
            }
            var handler = AccessTools.Field(typeof(LanguageManager), "_paramHandler").GetValue(manager);
            var dict = AccessTools.Field(typeof(LocalizationGlobalParameterHandler), "_replacements").GetValue(handler) as Dictionary<string, ReplacementStringData>;
            if (dict == null)
            {
                logger.Log(LogLevel.Error, "Unable to get replacement strings dictionary from Language Manager.");
                return;
            }
            foreach (var replacement in this.Values)
            {
                logger.Log(LogLevel.Debug, $"Adding Replacement ({replacement.Keyword}) -- ({replacement.ReplacementTextKey.LocalizeEnglish()})");
                dict.Add(replacement.Keyword, replacement);
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out ReplacementStringData? lookup, [NotNullWhen(true)] out bool? IsModded)
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
