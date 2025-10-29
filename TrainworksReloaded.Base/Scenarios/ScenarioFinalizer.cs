using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static SpawnGroupData;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Scenarios
{
    public class ScenarioDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<ScenarioDataFinalizer> logger;
        private readonly ICache<IDefinition<ScenarioData>> cache;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<GameObject> gameObjectRegister;
        private readonly IRegister<CharacterData> characterRegister;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<CardData> cardRegister;
        private readonly IRegister<BackgroundData> backgroundRegister;
        private readonly IRegister<BossVariantSpawnData> bossVariantRegister;
        private readonly IRegister<TrialDataList> trialListRegister;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly Lazy<SaveManager> saveManager;

        public ScenarioDataFinalizer(
            IModLogger<ScenarioDataFinalizer> logger,
            ICache<IDefinition<ScenarioData>> cache,
            GameDataClient client,
            IRegister<Sprite> spriteRegister,
            IRegister<GameObject> gameObjectRegister,
            IRegister<CharacterData> characterRegister,
            IRegister<RelicData> relicRegister,
            IRegister<CardData> cardRegister,
            IRegister<BackgroundData> backgroundRegister,
            IRegister<BossVariantSpawnData> bossVariantRegister,
            IRegister<TrialDataList> trialListRegister,
            IRegister<LocalizationTerm> termRegister)
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
            this.gameObjectRegister = gameObjectRegister;
            this.characterRegister = characterRegister;
            this.relicRegister = relicRegister;
            this.cardRegister = cardRegister;
            this.backgroundRegister = backgroundRegister;
            this.bossVariantRegister = bossVariantRegister;
            this.trialListRegister = trialListRegister;
            this.termRegister = termRegister;
            saveManager = new Lazy<SaveManager>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    return provider;
                }
                else
                {
                    return new();
                }
            });
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeScenario(definition);
            }
            cache.Clear();
        }

        public void FinalizeScenario(IDefinition<ScenarioData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var copyData = data;
            var key = definition.Key;

            var overrideMode = configuration.GetSection("override").ParseOverrideMode();

            logger.Log(LogLevel.Info, $"Finalizing Scenario {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var iconReference = configuration.GetSection("boss_icon").ParseReference();
            AccessTools.Field(typeof(ScenarioData), "bossIcon").SetValue(data, copyData.GetBossIcon());
            if (iconReference != null)
            {
                spriteRegister.TryLookupId(iconReference.ToId(key, TemplateConstants.Sprite), out var lookup, out var _, iconReference.context);
                AccessTools.Field(typeof(ScenarioData), "bossIcon").SetValue(data, lookup);
            }

            var portraitReference = configuration.GetSection("boss_portrait").ParseReference();
            AccessTools.Field(typeof(ScenarioData), "bossPortrait").SetValue(data, copyData.GetBossPortrait());
            if (portraitReference != null)
            {
                spriteRegister.TryLookupId(portraitReference.ToId(key, TemplateConstants.Sprite), out var lookup, out var _, portraitReference.context);
                AccessTools.Field(typeof(ScenarioData), "bossPortrait").SetValue(data, lookup);
            }

            var titanTrialReference = configuration.GetSection("titan_trial").ParseReference();
            AccessTools.Field(typeof(ScenarioData), "titanTrialSin").SetValue(data, copyData.GetTitanTrialSin());
            if (titanTrialReference != null)
            {
                var id = titanTrialReference.ToId(key, TemplateConstants.RelicData);
                relicRegister.TryLookupId(id, out var lookup, out var _, titanTrialReference.context);
                if (lookup is not SinsData)
                {
                    logger.Log(LogLevel.Warning, $"Enemy blessing given {id} is not a SinsData. Behavior may not be correct.");
                }
                AccessTools.Field(typeof(ScenarioData), "titanTrialSin").SetValue(data, lookup);
            }

            var backgroundReference = configuration.GetSection("background").ParseReference();
            AccessTools.Field(typeof(ScenarioData), "backgroundData").SetValue(data, copyData.GetBackgroundData());
            if (overrideMode.IsNewContent() || backgroundReference != null)
            {
                var id = backgroundReference?.ToId(key, TemplateConstants.Background) ?? "";
                backgroundRegister.TryLookupId(id, out var lookup, out var _, backgroundReference?.context);
                AccessTools.Field(typeof(ScenarioData), "backgroundData").SetValue(data, lookup);
            }

            var trialsReference = configuration.GetSection("trials").ParseReference();
            AccessTools.Field(typeof(ScenarioData), "trials").SetValue(data, copyData.GetTrialDataList());
            if (trialsReference != null)
            {
                var id = trialsReference.ToId(key, TemplateConstants.TrialList);
                trialListRegister.TryLookupId(id, out var lookup, out var _, trialsReference.context);
                AccessTools.Field(typeof(ScenarioData), "trials").SetValue(data, lookup);
            }

            var displayedEnemies = AccessTools.Field(typeof(ScenarioData), "displayedEnemies").GetValue(copyData) as List<CharacterData> ?? [];
            var displayedEnemyOffsets = copyData.GetDisplayedEnemyOffsets() ?? [];
            var displayedEnemiesConfig = configuration.GetSection("displayed_enemies");
            if (copyData != data)
            {
                displayedEnemies = [.. displayedEnemies];
                displayedEnemyOffsets = [.. displayedEnemyOffsets];
            }
            if (overrideMode == OverrideMode.Replace && displayedEnemiesConfig.Exists())
            {
                displayedEnemies.Clear();
                displayedEnemyOffsets.Clear();
            }
            foreach (var child in displayedEnemiesConfig.GetChildren())
            {
                var characterReference = child.GetSection("character").ParseReference();
                if (characterReference == null || !characterRegister.TryLookupName(characterReference.ToId(key, TemplateConstants.Character), out var lookup, out var _, characterReference.context))
                {
                    continue;
                }
                var offsetSection = configuration.GetSection("offset");
                Vector2 offset = new(0, 0);
                if (offsetSection != null)
                {
                    offset.x = offsetSection.GetSection("x").ParseInt() ?? 0;
                    offset.y = offsetSection.GetSection("y").ParseInt() ?? 0;
                }
                displayedEnemies.Add(lookup);
                displayedEnemyOffsets.Add(offset);
            }
            AccessTools.Field(typeof(ScenarioData), "displayedEnemies").SetValue(data, displayedEnemies);
            AccessTools.Field(typeof(ScenarioData), "displayedEnemyOffsets").SetValue(data, displayedEnemyOffsets);


            //handle discovery cards
            var cardDrawData = copyData.GetFtueCardDrawData();
            var cardDrawDataArray = AccessTools.Field(typeof(ScenarioData), "ftueCardDrawData").GetValue(data) as ReorderableArray<CardData>;
            if (copyData != data)
            {
                cardDrawData = [.. cardDrawData];
            }
            var ftueCardDrawDataConfig = configuration.GetSection("ftue_card_draws");
            if (overrideMode == OverrideMode.Replace && ftueCardDrawDataConfig.Exists())
            {
                cardDrawData.Clear();
            }
            var cardReferences = ftueCardDrawDataConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var cardReference in cardReferences)
            {
                if (
                    cardRegister.TryLookupName(
                        cardReference.ToId(key, TemplateConstants.Card),
                        out var card,
                        out var _,
                        cardReference.context
                    )
                )
                {
                    cardDrawData.Add(card);
                }
            }
            cardDrawDataArray!.CopyFrom(cardDrawData);
            AccessTools.Field(typeof(ScenarioData), "ftueCardDrawData").SetValue(data, cardDrawDataArray);

            var gameObjectReference = configuration.GetSection("prefab").ParseReference();
            AccessTools.Field(typeof(ScenarioData), "mapNodePrefab").SetValue(data, copyData.GetMapNodePrefab());
            if (gameObjectReference != null)
            {
                var id = gameObjectReference.ToId(key, TemplateConstants.GameObject);
                gameObjectRegister.TryLookupId(id, out var objectLookup, out var _, gameObjectReference.context);
                AccessTools.Field(typeof(ScenarioData), "mapNodePrefab").SetValue(data, objectLookup);
            }

            var bossVariantsConfig = configuration.GetSection("boss_variants");
            var bossVariantsReference = bossVariantsConfig.ParseReference();
            BossVariantSpawnData? bossVariantSpawnData = null;
            if (copyData != data && !bossVariantsConfig.Exists())
            {
                bossVariantSpawnData = AccessTools.Field(typeof(ScenarioData), "bossVariant").GetValue(copyData) as BossVariantSpawnData;
            }
            if (bossVariantsReference != null)
            {
                var id = bossVariantsReference.ToId(key, TemplateConstants.BossVariants);
                bossVariantRegister.TryLookupName(id, out bossVariantSpawnData, out var _, bossVariantsReference.context);
            }
            AccessTools.Field(typeof(ScenarioData), "bossVariant").SetValue(data, bossVariantSpawnData);

            List<RelicData> enemyBlessings = copyData.GetEnemyRelicData()?.ToList() ?? [];
            if (copyData != data)
                enemyBlessings = [.. enemyBlessings];
            var enemyBlessingConfig = configuration.GetSection("enemy_blessings");
            if (overrideMode == OverrideMode.Replace && enemyBlessingConfig.Exists())
            {
                enemyBlessings.Clear();
            }
            var enemyBlessingReferences = enemyBlessingConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in enemyBlessingReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RelicData);
                if (relicRegister.TryLookupName(id, out var relic, out var _, reference.context))
                {
                    if (relic is not SinsData)
                    {
                        logger.Log(LogLevel.Warning, $"Enemy blessing given {id} is not a SinsData. Behavior may not be correct.");
                    }
                    enemyBlessings.Add(relic);
                }
            }
            AccessTools.Field(typeof(ScenarioData), "enemyBlessingData").SetValue(data, enemyBlessings.ToArray());

            List<CharacterData> treasureCharacters = copyData.GetTreasureCharacterPool()?.ToList() ?? [];
            if (copyData != data)
                treasureCharacters = [.. treasureCharacters];
            var treasureCharacterPoolConfig = configuration.GetSection("treasure_character_pool");
            if (overrideMode == OverrideMode.Replace && treasureCharacterPoolConfig.Exists())
            {
                treasureCharacters.Clear();
            }
            var treasureCharacterReferences = treasureCharacterPoolConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in treasureCharacterReferences)
            {
                if (
                    characterRegister.TryLookupName(
                        reference.ToId(key, TemplateConstants.Character),
                        out var character,
                        out var _,
                        reference.context
                    )
                )
                {
                    treasureCharacters.Add(character);
                }
            }
            AccessTools.Field(typeof(ScenarioData), "treasureCharacterPool").SetValue(data, treasureCharacters.ToArray());

            ParseSpawnPattern(configuration.GetSection("spawn_pattern"), key, data, copyData, overrideMode);
        }

        private void ParseSpawnPattern(IConfiguration configuration, string key, ScenarioData scenario, ScenarioData copyScenario, OverrideMode overrideMode)
        {
            SpawnPatternData data = scenario.GetSpawnPattern() ?? new SpawnPatternData();
            // Do this now if scenario == copyScenario we get null.
            AccessTools.Field(typeof(ScenarioData), "spawnPattern").SetValue(scenario, data);
            SpawnPatternData copyData = copyScenario.GetSpawnPattern();

            var bossCharacterConfig = configuration.GetSection("boss_character");
            AccessTools.Field(typeof(SpawnPatternData), "bossCharacter").SetValue(data, copyData.GetOuterTrainBossCharacter());
            var bossCharacterReference = bossCharacterConfig.ParseReference();
            if (bossCharacterReference != null)
            {
                characterRegister.TryLookupName(bossCharacterReference.ToId(key, TemplateConstants.Character), out var character, out var _, bossCharacterReference.context);
                AccessTools.Field(typeof(SpawnPatternData), "bossCharacter").SetValue(data, character);
            }

            var hardBossCharacterConfig = configuration.GetSection("hard_boss_character");
            // Can't get this field directly always.
            var hardBoss = AccessTools.Field(typeof(SpawnPatternData), "hardBossCharacter").GetValue(copyData);
            AccessTools.Field(typeof(SpawnPatternData), "hardBossCharacter").SetValue(data, hardBoss);
            var hardBossCharacterReference = hardBossCharacterConfig.ParseReference();
            if (hardBossCharacterReference != null)
            {
                characterRegister.TryLookupName(hardBossCharacterReference.ToId(key, TemplateConstants.Character), out var character, out var _, hardBossCharacterReference.context);
                AccessTools.Field(typeof(SpawnPatternData), "hardBossCharacter").SetValue(data, character);
            }

            bool isLoopingScenario = copyData.GetIsLoopingScenario();
            AccessTools.Field(typeof(SpawnPatternData), "isLoopingScenario").SetValue(data, configuration.GetSection("is_looping_scenario").ParseBool() ?? isLoopingScenario);

            var bossType = copyData.GetBossType();
            AccessTools.Field(typeof(SpawnPatternData), "bossType").SetValue(data, configuration.GetSection("boss_type").ParseBossType() ?? bossType);

            ParseTrueFinalBoss(configuration.GetSection("true_final_bosses"), key, data, copyData, overrideMode);
            ParseSpawnGroupPools(configuration.GetSection("spawn_group_waves"), key, data, copyData, overrideMode);
        }

        private void ParseSpawnGroupPools(IConfiguration configuration, string key, SpawnPatternData data, SpawnPatternData copyData, OverrideMode overrideMode)
        {
            var spawnGroupWavesData = AccessTools.Field(typeof(SpawnPatternData), "spawnGroupWaves").GetValue(data) as SpawnPatternData.SpawnGroupPoolsDataList;
            var spawnGroupWavesCopy = AccessTools.Field(typeof(SpawnPatternData), "spawnGroupWaves").GetValue(copyData) as SpawnPatternData.SpawnGroupPoolsDataList;

            if (data != copyData)
            {
                foreach (var spawnGroupPoolCopy in spawnGroupWavesCopy!)
                {
                    var newSpawnGroupPool = new SpawnGroupPoolData();
                    var possibleGroups = AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(spawnGroupPoolCopy) as SpawnGroupPoolData.SpawnGroupDataList;
                    var possibleGroupsCopy = AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(spawnGroupPoolCopy) as SpawnGroupPoolData.SpawnGroupDataList;
                    foreach (var possibleGroup in possibleGroupsCopy!)
                    {
                        var newSpawnGroup = new SpawnGroupData();
                        AccessTools.Field(typeof(SpawnGroupData), "hasWaveMessage").SetValue(newSpawnGroup, possibleGroup.HasWaveMessage());
                        AccessTools.Field(typeof(SpawnGroupData), "waveMessageKey").SetValue(newSpawnGroup, AccessTools.Field(typeof(SpawnGroupData), "waveMessageKey").GetValue(possibleGroup));

                        var characterDataContainer = AccessTools.Field(typeof(SpawnGroupData), "characterDataContainerList").GetValue(newSpawnGroup) as SpawnGroupData.CharacterDataContainerList;
                        var characterDataContainerCopy = AccessTools.Field(typeof(SpawnGroupData), "characterDataContainerList").GetValue(possibleGroup) as SpawnGroupData.CharacterDataContainerList;

                        foreach (var characterDataItem in characterDataContainerCopy!)
                        {
                            var newCharacterContainer = new CharacterDataContainer();
                            AccessTools.Field(typeof(CharacterDataContainer), "characterData").SetValue(newCharacterContainer, characterDataItem.Character);
                            AccessTools.Field(typeof(CharacterDataContainer), "requiredCovenant").SetValue(newCharacterContainer, characterDataItem.Covenant);
                            AccessTools.Field(typeof(CharacterDataContainer), "suppressSpawn").SetValue(newCharacterContainer, characterDataItem.SuppressSpawn);
                            AccessTools.Field(typeof(CharacterDataContainer), "useBossCharacter").SetValue(newCharacterContainer, characterDataItem.UseBossCharacter);
                            characterDataContainer!.Add(newCharacterContainer);
                        }
                        possibleGroups!.Add(newSpawnGroup);
                    }
                    spawnGroupWavesData!.Add(newSpawnGroupPool);
                }
            }

            int i = 0;
            foreach (var spawnGroupPoolItem in configuration.GetChildren())
            {
                if (i >= spawnGroupWavesData!.Count)
                {
                    spawnGroupWavesData.Add(new());
                }
                var spawnGroupPoolData = spawnGroupWavesData[i];

                var possibleGroups = AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(spawnGroupPoolData) as SpawnGroupPoolData.SpawnGroupDataList;

                int j = 0;
                foreach (var possibleGroupsItem in spawnGroupPoolItem.GetSection("possible_groups").GetChildren())
                {
                    if (j >= possibleGroups!.Count)
                    {
                        possibleGroups.Add(new());
                    }
                    var spawnGroupData = possibleGroups[j];

                    ParseSpawnGroup(possibleGroupsItem, key, spawnGroupData, overrideMode);
                    j++;
                }
                i++;
            }
        }

        private void ParseSpawnGroup(IConfiguration configuration, string key, SpawnGroupData data, OverrideMode overrideMode)
        {
            var parseTerm = configuration.GetSection("wave_messages").ParseLocalizationTerm();
            if (parseTerm != null)
            {
                termRegister.Register(parseTerm.Key, parseTerm);
            }
            AccessTools.Field(typeof(SpawnGroupData), "hasWaveMessage").SetValue(data, parseTerm != null);
            AccessTools.Field(typeof(SpawnGroupData), "waveMessageKey").SetValue(data, parseTerm?.Key ?? "");

            var characterDataContainerList = AccessTools.Field(typeof(SpawnGroupData), "characterDataContainerList").GetValue(data) as SpawnGroupData.CharacterDataContainerList;
            var spawnListConfig = configuration.GetSection("spawn_list");
            if (overrideMode == OverrideMode.Replace && spawnListConfig.Exists())
            {
                characterDataContainerList!.Clear();
            }
            int i = 0;
            List<int> toDelete = [];
            foreach (var characterSpawnConfig in spawnListConfig.GetChildren())
            {
                if (i >= characterDataContainerList!.Count)
                {
                    characterDataContainerList.Add(new());
                }
                var characterContainer = characterDataContainerList[i];

                var characterReference = characterSpawnConfig.GetSection("character").ParseReference();
                if (characterReference != null)
                {
                    characterRegister.TryLookupName(characterReference.ToId(key, TemplateConstants.Character), out var lookup, out var _, characterReference.context);
                    AccessTools.Field(typeof(CharacterDataContainer), "characterData").SetValue(characterContainer, lookup);
                }
                if (characterContainer.Character == null)
                    toDelete.Add(i);

                var covenantLevel = characterSpawnConfig.GetSection("required_covenant").ParseInt() ?? -1;
                if (covenantLevel > 0)
                {
                    var covenantData = saveManager.Value.GetAllGameData().GetAscensionCovenantForLevel(covenantLevel);
                    AccessTools.Field(typeof(CharacterDataContainer), "requiredCovenant").SetValue(characterContainer, covenantData);
                }

                var suppressSpawn = characterSpawnConfig.GetSection("suppress_spawn").ParseBool() ?? characterContainer.SuppressSpawn;
                AccessTools.Field(typeof(CharacterDataContainer), "suppressSpawn").SetValue(characterContainer, suppressSpawn);

                var useBossCharacter = characterSpawnConfig.GetSection("use_boss_character").ParseBool() ?? characterContainer.UseBossCharacter;
                AccessTools.Field(typeof(CharacterDataContainer), "useBossCharacter").SetValue(characterContainer, useBossCharacter);
                i++;
            }
            foreach (int index in toDelete.Reverse<int>())
            {
                characterDataContainerList!.RemoveAt(index);
            }
        }

        private void ParseTrueFinalBoss(IConfigurationSection configuration, string key, SpawnPatternData data, SpawnPatternData copyData, OverrideMode overrideMode)
        {
            var trueFinalBossData = data.GetTrueFinalBosses() ?? [];
            AccessTools.Field(typeof(SpawnPatternData), "trueFinalBosses").SetValue(data, trueFinalBossData);
            var trueFinalBossCopy = copyData.GetTrueFinalBosses() ?? [];
            if (data != copyData)
            {
                foreach (var characterList in trueFinalBossCopy)
                {
                    SpawnPatternData.TrueFinalBoss newTfb = new();
                    newTfb.characters.AddRange(characterList.characters);
                    trueFinalBossData.Add(newTfb);
                }
            }

            int i = 0;
            foreach (var tfbItem in configuration.GetChildren())
            {
                if (i >= trueFinalBossData.Count)
                {
                    trueFinalBossData.Add(new());
                }
                var tfbData = trueFinalBossData[i];

                int j = 0;
                foreach (var characterConfig in tfbItem.GetChildren())
                {
                    var characterReference = characterConfig.ParseReference();
                    CharacterData? characterFound = null;

                    if (characterReference != null)
                    {
                        characterRegister.TryLookupName(characterReference.ToId(key, TemplateConstants.Character), out characterFound, out var _, characterReference.context);
                    }

                    if (j >= tfbData.characters.Count)
                    {
                        tfbData.characters.Add(characterFound);
                    }
                    else
                    {
                        tfbData.characters[j] = characterFound;
                    }
                    j++;
                }
                tfbData.characters.RemoveAll(character => character == null);
                i++;
            }
        }
    }
}
