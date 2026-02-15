using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterDataPipeline : IDataPipeline<IRegister<CharacterData>, CharacterData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<CharacterDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;

        public CharacterDataPipeline(
            PluginAtlas atlas,
            IModLogger<CharacterDataPipeline> logger,
            IRegister<LocalizationTerm> termRegister,
            IGuidProvider guidProvider
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
        }

        public List<IDefinition<CharacterData>> Run(IRegister<CharacterData> service)
        {
            var processList = new List<IDefinition<CharacterData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(
                    LoadCharacters(service, config.Key, config.Value.Configuration)
                );
            }
            return processList;
        }

        /// <summary>
        /// Loads the Character Definitions
        /// </summary>
        /// <param name="service"></param>
        /// <param name="key"></param>
        /// <param name="pluginConfig"></param>
        /// <returns></returns>
        private List<CharacterDataDefinition> LoadCharacters(
            IRegister<CharacterData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<CharacterDataDefinition>();
            foreach (var child in pluginConfig.GetSection("characters").GetChildren())
            {
                var data = LoadCharacterConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private CharacterDataDefinition? LoadCharacterConfiguration(
            IRegister<CharacterData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.Character, id);
            var namekey = $"CharacterData_nameKey-{name}";
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();
            var cloneId = configuration.GetSection("clone_id").Value;

            string guid;
            CharacterData copyData;
            CharacterData data;
            if (cloneId != null)
            {
                logger.Log(LogLevel.Debug, $"Cloning Character {cloneId}...");
                service.TryLookupName(cloneId, out var cloneData, out var _);
                data = ScriptableObject.CreateInstance<CharacterData>();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
                copyData = cloneData ?? data;
            }
            else if (overrideMode.IsOverriding() && service.TryLookupName(id, out data!, out var _))
            {
                logger.Log(LogLevel.Info, $"Overriding Character {id}...");
                namekey = data.GetNameKey();
                guid = data.GetID();
                copyData = data;
            }
            else
            {
                data = ScriptableObject.CreateInstance<CharacterData>();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
                copyData = data;
            }
            //handle id
            AccessTools.Field(typeof(CharacterData), "id").SetValue(data, guid);

            //handle names
            AccessTools.Field(typeof(CharacterData), "nameKey").SetValue(data, copyData.GetNameKey());
            var localizationNameTerm = configuration.GetSection("names").ParseLocalizationTerm();
            if (localizationNameTerm != null)
            {
                AccessTools.Field(typeof(CharacterData), "nameKey").SetValue(data, namekey);
                localizationNameTerm.Key = namekey;
                termRegister.Register(namekey, localizationNameTerm);
            }

            //bools
            var hideInLogbook = copyData.GetHideInLogbook();
            AccessTools
                .Field(typeof(CharacterData), "hideInLogbook")
                .SetValue(
                    data,
                    configuration.GetSection("hide_in_logbook").ParseBool() ?? hideInLogbook
                );

            var blockVisualSizeIncrease = copyData.BlockVisualSizeIncrease();
            AccessTools
                .Field(typeof(CharacterData), "blockVisualSizeIncrease")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("block_size_increase", "block_visual_size_increase").ParseBool()
                        ?? blockVisualSizeIncrease
                );

            var canBeHealed = copyData.GetCanBeHealed();
            AccessTools
                .Field(typeof(CharacterData), "canBeHealed")
                .SetValue(
                    data,
                    configuration.GetSection("can_be_healed").ParseBool() ?? canBeHealed
                );

            var isOuterTrainBoss = copyData.IsOuterTrainBoss();
            AccessTools
                .Field(typeof(CharacterData), "isOuterTrainBoss")
                .SetValue(
                    data,
                    configuration.GetSection("is_outer_train_boss").ParseBool() ?? isOuterTrainBoss
                );

            var isMiniboss = copyData.IsMiniboss();
            AccessTools
                .Field(typeof(CharacterData), "isMiniboss")
                .SetValue(data, configuration.GetSection("is_mini_boss").ParseBool() ?? isMiniboss);

            var isCompanionBoss = copyData.IsCompanionBoss();
            AccessTools
                .Field(typeof(CharacterData), "isCompanionBoss")
                .SetValue(data, configuration.GetSection("is_companion_boss").ParseBool() ?? isCompanionBoss);

            var canAttack = copyData.GetCanAttack();
            AccessTools
                .Field(typeof(CharacterData), "canAttack")
                .SetValue(data, configuration.GetSection("can_attack").ParseBool() ?? canAttack);

            var ascendsTrainAutomatically = copyData.GetAscendsTrainAutomatically();
            AccessTools
                .Field(typeof(CharacterData), "ascendsTrainAutomatically")
                .SetValue(
                    data,
                    configuration.GetSection("ascends_train_automatically").ParseBool()
                        ?? ascendsTrainAutomatically
                );

            var loopsBetweenTrainFloors = copyData.GetLoopsBetweenTrainFloors();
            AccessTools
                .Field(typeof(CharacterData), "loopsBetweenTrainFloors")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("loops_between_floors", "loops_between_train_floors").ParseBool()
                        ?? loopsBetweenTrainFloors
                );

            var attackTeleportsToDefender = copyData.IsAttackTeleportsToDefender();
            AccessTools
                .Field(typeof(CharacterData), "attackTeleportsToDefender")
                .SetValue(
                    data,
                    configuration.GetSection("attack_teleports_to_defender").ParseBool()
                        ?? attackTeleportsToDefender
                );

            var deathSlidesBackwards = copyData.IsDeathSlidesBackwards();
            AccessTools
                .Field(typeof(CharacterData), "deathSlidesBackwards")
                .SetValue(
                    data,
                    configuration.GetSection("death_slides_backwards").ParseBool()
                        ?? deathSlidesBackwards
                );

            var chosenVariant = copyData.IsChosenVariant();
            AccessTools
                .Field(typeof(CharacterData), "chosenVariant")
                .SetValue(
                    data,
                    configuration.GetSection("chosen_variant").ParseBool() ?? chosenVariant
                );

            var lifemotherVariant = copyData.IsLifemotherVariant();
            AccessTools
                .Field(typeof(CharacterData), "lifemotherVariant")
                .SetValue(
                    data,
                    configuration.GetSection("lifemother_variant").ParseBool() ?? lifemotherVariant
                );

            var isPyreHeart = copyData.IsPyreHeart();
            AccessTools
                .Field(typeof(CharacterData), "isPyreHeart")
                .SetValue(data, configuration.GetDeprecatedSection("is_pyre", "is_pyre_heart").ParseBool() ?? isPyreHeart);

            var disableInDailyChallenges = copyData.GetDisableInDailyChallenges();
            AccessTools
                .Field(typeof(CharacterData), "disableInDailyChallenges")
                .SetValue(
                    data,
                    configuration.GetSection("disable_in_daily_challenges").ParseBool()
                        ?? disableInDailyChallenges
                );

            var preventAbilitiesFromEquipment = copyData.GetPreventAblitiesFromEquipment();
            AccessTools
                .Field(typeof(CharacterData), "preventAbilitiesFromEquipment")
                .SetValue(
                    data,
                    configuration.GetSection("prevent_abilities_from_equipment").ParseBool()
                        ?? preventAbilitiesFromEquipment
                );

            //int
            var size = copyData.GetSize();
            AccessTools
                .Field(typeof(CharacterData), "size")
                .SetValue(data, configuration.GetSection("size").ParseInt() ?? size);

            var health = overrideMode.IsNewContent() ? 0 : copyData.GetHealth();
            AccessTools
                .Field(typeof(CharacterData), "health")
                .SetValue(data, configuration.GetSection("health").ParseInt() ?? health);

            var attackDamage = overrideMode.IsNewContent() ? 0 : copyData.GetAttackDamage();
            AccessTools
                .Field(typeof(CharacterData), "attackDamage")
                .SetValue(
                    data,
                    configuration.GetSection("attack_damage").ParseInt() ?? attackDamage
                );

            var equipmentLimit = copyData.GetEquipmentLimit();
            AccessTools
                .Field(typeof(CharacterData), "equipmentLimit")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("equip_limit", "equipment_limit").ParseInt() ?? equipmentLimit
                );

            //attack phase
            var validBossAttackPhase = copyData.GetValidBossAttackPhase();
            AccessTools
                .Field(typeof(CharacterData), "validBossAttackPhase")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("valid_attack_phase", "valid_boss_attack_phase").ParseAttackPhase()
                        ?? validBossAttackPhase
                );

            //death type
            var deathType = copyData.GetDeathType();
            AccessTools
                .Field(typeof(CharacterData), "deathType")
                .SetValue(
                    data,
                    configuration.GetSection("death_type").ParseCharacterDeathType() ?? deathType
                );

            //death type
            var bossTitanAffinity = overrideMode.IsNewContent() ? TitanAffinity.None :
                    copyData.GetBossTitanAffinity();
            AccessTools
                .Field(typeof(CharacterData), "bossTitanAffinity")
                .SetValue(
                    data,
                    configuration.GetDeprecatedSection("titan_affinity", "boss_titan_affinity").ParseTitanAffinity()
                        ?? bossTitanAffinity
                );

            //handle tooltips
            var tooltips = (List<String>)copyData.GetCharacterLoreTooltipKeys() ?? [];
            if (copyData != data)
                tooltips = [.. tooltips];
            var loreTooltipsSection = configuration.GetDeprecatedSection("character_lore_tooltips", "lore_tooltips");
            if (overrideMode == OverrideMode.Replace && loreTooltipsSection.Exists())
            {
                tooltips.Clear();
            }
            int tooltip_count = tooltips.Count;
            foreach (var tooltip in loreTooltipsSection.GetChildren())
            {
                var localizationTooltipTerm = tooltip.ParseLocalizationTerm();
                if (localizationTooltipTerm != null)
                {
                    string tooltipKey = $"CharacterData_tooltipKey{tooltip_count}-{name}";
                    if (localizationTooltipTerm.Key.IsNullOrEmpty())
                        localizationTooltipTerm.Key = tooltipKey;
                    tooltips.Add(tooltipKey);
                    if (localizationTooltipTerm.HasTranslation())
                        termRegister.Register(tooltipKey, localizationTooltipTerm);
                    tooltip_count++;
                }
            }
            AccessTools.Field(typeof(CharacterData), "characterLoreTooltipKeys").SetValue(data, tooltips);

            var artistAttribution = copyData.GetArtistAttribution();
            AccessTools
                .Field(typeof(CharacterData), "artistAttribution")
                .SetValue(
                    data,
                    configuration.GetSection("artist").ParseString() ?? artistAttribution
                );

            //endless baseline stats
            var endlessBaselineStats = copyData.GetEndlessBaselineStats();
            var endlessHealth = configuration
                .GetSection("endless_stats")
                .GetSection("health")
                .ParseInt();
            if (endlessHealth != null)
            {
                AccessTools
                    .Field(typeof(EndlessBaselineStats), "endlessHealth")
                    .SetValue(endlessBaselineStats, endlessHealth);
            }
            var endlessAttack = configuration
                .GetSection("endless_stats")
                .GetSection("attack")
                .ParseInt();
            if (endlessHealth != null)
            {
                AccessTools
                    .Field(typeof(EndlessBaselineStats), "endlessAttack")
                    .SetValue(endlessBaselineStats, endlessAttack);
            }
            AccessTools
                .Field(typeof(CharacterData), "endlessBaselineStats")
                .SetValue(data, endlessBaselineStats);

            //register before filling in data using
            var modded = cloneId != null || overrideMode.IsNewContent();
            if (modded)
                service.Register(name, data);

            return new CharacterDataDefinition(key, data, copyData, configuration, overrideMode, modded)
            {
                Id = id
            };
        }
    }
}
