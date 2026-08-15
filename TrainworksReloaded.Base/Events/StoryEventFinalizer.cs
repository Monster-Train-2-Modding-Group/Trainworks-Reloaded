using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Relic;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Events
{
    public class StoryEventFinalizer : IDataFinalizer
    {
        private readonly PluginAtlas atlas;
        private readonly SaveManager saveManager;
        private readonly IModLogger<StoryEventFinalizer> logger;
        private readonly ICache<IDefinition<StoryEventData>> cache;
        private readonly Lazy<TextAsset> MasterStoryFile;
        private readonly Lazy<StoryManager> StoryManager;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<StoryEventData> storyRegister;
        private readonly IRegister<RewardData> rewardRegister;
        private readonly IRegister<CardData> cardRegister;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IRegister<AssetReferenceGameObject> assetReferenceRegister;
        private readonly IRegister<StoryEventPoolData> poolRegister;
        private readonly IRegister<LocalizationTerm> termRegister;
        
        private readonly FieldInfo StoryEventDataEventPrefabField = AccessTools.Field(typeof(StoryEventData), "eventPrefab");
        private readonly FieldInfo StoryEventPoolStoryListField = AccessTools.Field(typeof(StoryEventPoolData), "storyEvents");
        private readonly AssetReferenceGameObject? DefaultEventPrefab;

        private readonly IDictionary<string, JArray> knotsToAdd = new Dictionary<string, JArray>();
        
        public StoryEventFinalizer(
            PluginAtlas atlas,
            IModLogger<StoryEventFinalizer> logger,
            GameDataClient client,
            ICache<IDefinition<StoryEventData>> cache,
            IRegister<RelicData> relicRegister,
            IRegister<StoryEventData> storyRegister,
            IRegister<RewardData> rewardRegister,
            IRegister<CardData> cardRegister,
            IRegister<CardUpgradeData> upgradeRegister,
            IRegister<StoryEventPoolData> poolRegister,
            IRegister<LocalizationTerm> termRegister,
            IRegister<AssetReferenceGameObject> assetReferenceRegister
        )
        {
            this.atlas = atlas;
            client.TryGetProvider<SaveManager>(out saveManager!);
            StoryManager = new Lazy<StoryManager>(() =>
            {
                if (client.TryGetProvider<StoryManager>(out var provider))
                {
                    return provider;
                }
                else
                {
                    return new StoryManager();
                }
            });
            MasterStoryFile = new Lazy<TextAsset>(() =>
            {
                return saveManager.GetAllGameData().GetBalanceData().MasterStoryFile;
            });
            this.logger = logger;
            this.cache = cache;
            this.relicRegister = relicRegister;
            this.storyRegister = storyRegister;
            this.rewardRegister = rewardRegister;
            this.cardRegister = cardRegister;
            this.upgradeRegister = upgradeRegister;
            this.assetReferenceRegister = assetReferenceRegister;
            this.poolRegister = poolRegister;
            this.termRegister = termRegister;
            DefaultEventPrefab = StoryEventDataEventPrefabField.GetValue(saveManager.GetAllGameData().FindStoryEventDataByName("TextAnimationsTestEvent")) as AssetReferenceGameObject;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeItem(definition);
            }
            cache.Clear();
            string? newMasterStoryFile = InjectIntoMasterStoryFile();
            if (newMasterStoryFile != null)
            {
                BalanceData balanceData = saveManager.GetAllGameData().GetBalanceData();
                AccessTools.Field(typeof(BalanceData), "masterStoryFile").SetValue(balanceData, new TextAsset(newMasterStoryFile));
                // Necessary to bring in the new master story file.
                StoryManager.Value.InkWriter.Initialize(balanceData.MasterStoryFile);
            }
        }

        private void FinalizeItem(IDefinition<StoryEventData> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var data = definition.Data;

            logger.Log(LogLevel.Info, $"Finalizing StoryEvent {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var mutatorReference = configuration.GetSection("excluded_mutator").ParseReference();
            if (mutatorReference != null)
            {
                relicRegister.TryLookupName(mutatorReference.ToId(key, TemplateConstants.RelicData), out var lookup, out var _, mutatorReference.context);
                if (lookup is not MutatorData)
                {
                    logger.Log(LogLevel.Warning, $"Relic data name: {lookup?.name} given is not a MutatorData ignoring.");
                }
                AccessTools.Field(typeof(StoryEventData), "excludedMutator").SetValue(data, lookup as MutatorData ?? null);
            }

            List<StoryEventData> excludedEvents = [];
            var eventReferences = configuration.GetSection("excluded_events")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var eventReference in eventReferences)
            {
                var eventId = eventReference.ToId(key, TemplateConstants.StoryEvent);
                if (storyRegister.TryLookupName(eventId, out var eventData, out var _, eventReference.context))
                {
                    excludedEvents.Add(eventData);
                }
            }
            AccessTools.Field(typeof(StoryEventData), "excludingEventData").SetValue(data, excludedEvents);

            List<RewardData> possibleRewards = [];
            var rewardReferences = configuration.GetSection("possible_rewards")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var rewardReference in rewardReferences)
            {
                var id = rewardReference.ToId(key, TemplateConstants.RewardData);
                if (rewardRegister.TryLookupName(id, out var rewardData, out var _, rewardReference.context))
                {
                    possibleRewards.Add(rewardData);
                }
            }
            AccessTools.Field(typeof(StoryEventData), "possibleRewards").SetValue(data, possibleRewards);


            ParseFollowupEvents(key, configuration.GetSection("followup_events"), data);

            var prefabReference = configuration.GetSection("prefab").ParseAssetReference();
            AssetReferenceGameObject? prefab = DefaultEventPrefab!;
            if (prefabReference != null)
            {
                if (assetReferenceRegister.TryLookupId(
                        prefabReference.ToId(key, TemplateConstants.GameObject),
                        out var gameObject,
                        out var _,
                        prefabReference.context))
                {
                    prefab = gameObject;
                }
            }
            StoryEventDataEventPrefabField.SetValue(data, prefab);

            var prefabReference2 = configuration.GetSection("soul_savior_prefab").ParseAssetReference();
            if (prefabReference2 != null)
            {
                if (
                    assetReferenceRegister.TryLookupId(
                        prefabReference2.ToId(key, TemplateConstants.GameObject),
                        out var gameObject,
                        out var _,
                        prefabReference2.context
                    )
                )
                {
                    AccessTools
                        .Field(typeof(StoryEventData), "regionRunEventPrefabVariant")
                        .SetValue(data, gameObject);
                }
            }

            var poolReferences = configuration.GetSection("pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>().ToList();

            var isFollowupEventConfig = configuration.GetSection("is_followup_event");
            var is_followup_event = isFollowupEventConfig.ParseBool() ?? false;
            if (!is_followup_event)
            {
                poolReferences.Add(new ReferencedObject("StoryEventPoolData", null, isFollowupEventConfig));
            }

            foreach (var poolReference in poolReferences)
            {
                var id = poolReference.ToId(key, TemplateConstants.StoryEventPool);
                if (poolRegister.TryLookupName(id, out var pool, out var _, poolReference.context))
                {
                    var storyList = StoryEventPoolStoryListField.GetValue(pool) as ReorderableArray<StoryEventData>;
                    storyList?.Add(data);
                    logger.Log(LogLevel.Debug, $"Added event {definition.Id.ToId(key, TemplateConstants.StoryEvent)} to pool: {pool}");
                }
            }

            // Translations for Event choices goes to main localizations.
            foreach (var choiceConfig in configuration.GetSection("choice_texts").GetChildren())
            {
                var choice = choiceConfig.GetSection("choice").ParseString();
                var term = choiceConfig.GetSection("texts").ParseLocalizationTerm();
                if (term != null)
                {
                    term.Key = $"EventChoice_{data.KnotName}_{choice}";
                    termRegister.Register(term.Key, term);
                }
                List<string> objects = [];
                foreach (var info in choiceConfig.GetSection("preview_infos").GetChildren())
                {
                    var previewType = ParsePreviewType(info.GetSection("preview_type").ParseString());
                    var references = info.GetSection("references").ParseReferences();
                    objects.Add(FormOptionalCommandText(key, previewType, references));
                }
                var command = choiceConfig.GetSection("preview_obtain_texts").ParseLocalizationTerm();
                if (command != null)
                {
                    command.Key = $"EventChoice_{data.KnotName}_{choice}_Optional";
                    command.Format(objects);
                    termRegister.Register(command.Key, command);
                }
            }

            var path = configuration.GetSection("story_data").ParseString();
            string? fullpath = null;
            foreach (var directory in atlas.PluginDefinitions[key].AssetDirectories)
            {
                fullpath = Path.Combine(directory, path);
                if (!File.Exists(fullpath))
                {
                    logger.Log(LogLevel.Error, $"Could not find asset at path: {fullpath}. StoryEventData is invalid.");
                    continue;
                }
            }

            if (fullpath == null)
                return;

            var knot = ExtractKnotFromFile(fullpath, data.KnotName);
            if (knot == null) return;
            ProcessMacroTokens(knot, key);

            knotsToAdd.Add(data.KnotName, knot);
        }

        private string FormOptionalCommandText(string key, StoryChoiceData.PreviewType previewType, IEnumerable<ReferencedObject?> references)
        {
            List<string> objects = [];
            switch (previewType)
            {
                case StoryChoiceData.PreviewType.Card:
                    foreach (var reference in references)
                    {
                        if (reference == null)
                        {
                            objects.Add(string.Empty);
                            continue;
                        }
                        cardRegister.TryLookupName(reference.ToId(key, TemplateConstants.Card), out var lookup, out var _, reference.context);
                        objects.Add(lookup?.name ?? string.Empty);
                    }
                    break;
                case StoryChoiceData.PreviewType.Relic:
                case StoryChoiceData.PreviewType.Relic_Name:
                case StoryChoiceData.PreviewType.Upgrade:
                    foreach (var reference in references)
                    {
                        if (reference == null)
                        {
                            objects.Add(string.Empty);
                            continue;
                        }
                        relicRegister.TryLookupName(reference.ToId(key, TemplateConstants.RelicData), out var lookup, out var _, reference.context);
                        objects.Add(lookup?.name ?? string.Empty);
                    }
                    break;
                case StoryChoiceData.PreviewType.None:
                case StoryChoiceData.PreviewType.Reward:
                case StoryChoiceData.PreviewType.DeckRewards:
                case StoryChoiceData.PreviewType.DeckReward:
                case StoryChoiceData.PreviewType.DelayedEnhanceReward:
                    foreach (var reference in references)
                    {
                        if (reference == null)
                        {
                            objects.Add(string.Empty);
                            continue;
                        }
                        rewardRegister.TryLookupName(reference.ToId(key, TemplateConstants.RewardData), out var lookup, out var _, reference.context);
                        objects.Add(lookup?.name ?? string.Empty);
                    }
                    break;
                case StoryChoiceData.PreviewType.Coin:
                    foreach (var reference in references)
                    {
                        objects.Add(reference?.id ?? "0");
                    }
                    break;
            }

            return $"{{{previewType}: {String.Join(',', objects)}}}";
        }

        private StoryChoiceData.PreviewType ParsePreviewType(string? v)
        {
            if (string.IsNullOrEmpty(v))
            {
                return StoryChoiceData.PreviewType.None;
            }
            return v.ToLower() switch
            {
                "none" => StoryChoiceData.PreviewType.None,
                "card" => StoryChoiceData.PreviewType.Card,
                "relic" => StoryChoiceData.PreviewType.Relic,
                "enhancer" => StoryChoiceData.PreviewType.Upgrade,
                "reward" => StoryChoiceData.PreviewType.Reward,
                "coin" => StoryChoiceData.PreviewType.Coin,
                "deck_reward" => StoryChoiceData.PreviewType.DeckReward,
                "delayed_enhance_reward" => StoryChoiceData.PreviewType.DelayedEnhanceReward,
                "relic_name" => StoryChoiceData.PreviewType.Relic_Name,
                "deck_rewards" => StoryChoiceData.PreviewType.DeckRewards,
                _ => StoryChoiceData.PreviewType.None,
            };
        }

        /// <summary>
        /// Finds a specific knot inside a compiled Ink JSON file and extracts its array structure.
        /// </summary>
        /// <param name="jsonFilePath">Path to your compiled payload json file</param>
        /// <param name="knotName">The name of the knot to extract (e.g., "my_injected_event")</param>
        /// <returns>A JArray representing the standalone bytecode of the knot</returns>
        public JArray? ExtractKnotFromFile(string jsonFilePath, string knotName)
        {
            // 1. Read and parse the payload JSON
            string rawJson = File.ReadAllText(jsonFilePath);
            JObject payloadDoc = JObject.Parse(rawJson);

            // 2. Get the root container array where all knots live
            if (payloadDoc["root"] is not JArray rootArray)
            {
                logger.Log(LogLevel.Error, $"Invalid Ink JSON {jsonFilePath}. Missing 'root' container.");
                return null;
            }

            // 3. Iterate through the elements to find the array belonging to our knot
            foreach (var element in rootArray)
            {
                if (element is JObject knotDictionary)
                {
                    if (knotDictionary.TryGetValue(knotName, out JToken? knotToken))
                    {
                        if (knotToken is JArray knotArray)
                        {
                            return knotArray.DeepClone() as JArray;
                        }
                    }
                }
            }

            logger.Log(LogLevel.Error, $"Knot with name '{knotName}' was not found in the Ink JSON file: {jsonFilePath}");
            return null;
        }

        private void ParseFollowupEvents(string key, IConfiguration configuration, StoryEventData data)
        {
            List<FollowupEventData> events = [];
            foreach (var child in configuration.GetChildren())
            {
                events.Add(ParseFollowupEvent(key, child));
            }
            AccessTools.Field(typeof(StoryEventData), "followupEvents").SetValue(data, events);
        }

        private FollowupEventData ParseFollowupEvent(string key, IConfigurationSection configuration)
        {
            var data = new FollowupEventData();

            var eventReference = configuration.GetSection("event").ParseReference();
            if (eventReference != null)
            {
                storyRegister.TryLookupName(eventReference.ToId(key, TemplateConstants.StoryEvent), out var lookup, out var _, eventReference.context);
                AccessTools.Field(typeof(FollowupEventData), "followupEvent").SetValue(data, lookup ?? null);
            }

            AccessTools.Field(typeof(FollowupEventData), "canShowAfterVictory").SetValue(data, configuration.GetSection("can_show_after_victory").ParseBool() ?? false);

            List<FollowupConditionData> conditions = [];
            foreach (var child in configuration.GetSection("conditions").GetChildren())
            {
                var cond = ParseFollowupCondition(key, child);
                if (cond != null)
                    conditions.Add(cond);
            }
            AccessTools.Field(typeof(FollowupEventData), "conditions").SetValue(data, conditions);

            return data;
        }

        private FollowupConditionData? ParseFollowupCondition(string key, IConfigurationSection configuration)
        {
            var data = new FollowupConditionData();

            var classReference = configuration.GetSection("name").ParseReference();
            if (classReference == null)
                return null;

            var stateName = classReference.id;
            var modReference = classReference.mod_reference ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !stateName.GetFullyQualifiedName<FollowupConditionBase>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                logger.Log(LogLevel.Error, $"Failed to load condition state name {stateName} in {classReference.context} mod {modReference}, Make sure the class exists in {modReference} and that the class inherits from FollowupConditionBase.");
                return null;
            }
            AccessTools
                .Field(typeof(FollowupConditionData), "conditionName")
                .SetValue(data, fullyQualifiedName);

            var paramStr = "";
            AccessTools
                .Field(typeof(FollowupConditionData), "paramString")
                .SetValue(data, configuration.GetSection("param_str").ParseString() ?? paramStr);

            var paramInt = 0;
            AccessTools
                .Field(typeof(FollowupConditionData), "paramInt")
                .SetValue(data, configuration.GetSection("param_int").ParseInt() ?? paramInt);

            var additionalParamInt = 0;
            AccessTools
                .Field(typeof(FollowupConditionData), "paramAdditionalInt")
                .SetValue(
                    data,
                    configuration.GetSection("param_int_2").ParseInt() ?? additionalParamInt
                );

            var upgradeReference = configuration.GetSection("param_upgrade").ParseReference();
            if (
                upgradeReference != null
                && upgradeRegister.TryLookupName(
                    upgradeReference.ToId(key, TemplateConstants.Upgrade),
                    out var upgradeData,
                    out var _,
                    upgradeReference.context
                )
            )
            {
                AccessTools
                    .Field(typeof(FollowupConditionData), "paramCardUpgradeData")
                    .SetValue(data, upgradeData);
            }

            var paramRelicReference = configuration.GetSection("param_relic").ParseReference();
            if (paramRelicReference != null)
            {
                relicRegister.TryLookupName(
                    paramRelicReference.ToId(key, TemplateConstants.RelicData),
                    out var lookup,
                    out var _, paramRelicReference.context);
                AccessTools.Field(typeof(FollowupConditionData), "paramRelicData").SetValue(data, lookup);
            }

            var cardReference = configuration.GetSection("param_card").ParseReference();
            CardData? card = null;
            if (cardReference != null)
            {
                cardRegister.TryLookupName(cardReference.ToId(key, TemplateConstants.Card), out card, out var _, cardReference.context);
            }
            AccessTools
                .Field(typeof(FollowupConditionData), "paramCardData")
                .SetValue(data, card);

            data.Initialize();
            return data;
        }

        // Matches lines starting with >>> and captures the ID following the '@'
        // Pattern: Starts with '>>>', followed by anything up to '@', then captures word/id chars after '@'
        private static readonly Regex MacroRegex = new(@"^>>>(.*)?(@[A-Za-z0-9_]+)", RegexOptions.Compiled);

        /// <summary>
        /// Scans a knot JArray for '>>>' lines with '@' references, resolves them via .ToId(), 
        /// and replaces the token in place.
        /// </summary>
        public void ProcessMacroTokens(JToken token, string key)
        {
            if (token is JValue value && value.Type == JTokenType.String)
            {
                string rawStr = value.Value<string>()!;

                // Check if this is an Ink string token starting with ^>>>
                // Ink prepends '^' to plain text lines
                if (rawStr.StartsWith("^>>>"))
                {
                    string cleanText = rawStr.Substring(1); // Remove the '^' prefix for processing

                    Match match = MacroRegex.Match(cleanText);
                    if (match.Success)
                    {
                        string command = match.Groups[1].Value.Trim();
                        string rewardId = match.Groups[2].Value;

                        var template = command switch
                        {
                            "GIVE_REWARD" => TemplateConstants.RewardData,
                            "REMOVE_CARD" => TemplateConstants.Card,
                            "REMOVE_RELIC" => TemplateConstants.RelicData,
                            _ => TemplateConstants.RewardData
                        };

                        string expandedId = rewardId.ToId(key, template);

                        string updatedText = cleanText.Replace(rewardId, expandedId);

                        logger.Log(LogLevel.Error, $"{command} {rewardId} -> {expandedId} = {updatedText}");

                        // Preserve the '^' prefix required by the Ink Virtual Machine
                        value.Value = "^" + updatedText;
                    }
                }
            }
            else if (token is JArray array)
            {
                foreach (var child in array)
                {
                    ProcessMacroTokens(child, key);
                }
            }
            else if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    ProcessMacroTokens(property.Value, key);
                }
            }
        }
        private string? InjectIntoMasterStoryFile()
        {
            if (knotsToAdd.IsNullOrEmpty()) return null;

            // 1. Read and parse the master story file
            JObject masterDoc = JObject.Parse(MasterStoryFile.Value.text);

            // 2. Get the root container array
            if (masterDoc["root"] is not JArray rootArray)
            {
                logger.Log(LogLevel.Error, $"Invalid Ink JSON. Missing 'root' container.");
                return null;
            }

            // 3. Find the knot dictionary inside the root array
            JObject? masterKnotDictionary = null;
            foreach (var element in rootArray)
            {
                // The knot dictionary is a JObject that contains a function flag ("#f")
                if (element is JObject obj && obj["#f"] != null)
                {
                    masterKnotDictionary = obj;
                    break;
                }
            }

            if (masterKnotDictionary == null)
            {
                logger.Log(LogLevel.Error, "Could not locate the knot definition dictionary inside the master story file.");
                return null;
            }

            // 4. Merge the custom knots into the master dictionary
            foreach (var knot in knotsToAdd)
            {
                string knotName = knot.Key;
                JArray knotBytecode = knot.Value;
                if (masterKnotDictionary.ContainsKey(knotName))
                {
                    logger.Log(LogLevel.Warning, $"Overwriting existing knot in master file: {knotName}");
                }
                masterKnotDictionary[knotName] = knotBytecode;
            }

            // 5. Serialize back to the file
            // Formatting.None keeps file size small; use Formatting.Indented if you want to inspect it by hand.
            return masterDoc.ToString(Formatting.None);
        }
    }
}
