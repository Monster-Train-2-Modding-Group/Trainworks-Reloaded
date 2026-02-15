using HarmonyLib;
using Microsoft.Extensions.Configuration;
using Mono.Cecil;
using ShinyShoe.Audio;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static ShinyShoe.Audio.CoreSoundEffectData;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CharacterDataFinalizer> logger;
        private readonly ICache<IDefinition<CharacterData>> cache;
        private readonly IRegister<AssetReferenceGameObject> assetReferenceRegister;
        private readonly IRegister<GameObject> gameObjectRegister;
        private readonly IRegister<CharacterTriggerData> triggerRegister;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<CardData> cardRegister;
        private readonly IRegister<CharacterChatterData> chatterRegister;
        private readonly IRegister<SubtypeData> subtypeRegister;
        private readonly IRegister<RoomModifierData> roomModifierRegister;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<SoundCueDefinition> soundCueRegister;
        private readonly IRegister<PyreHeartData> pyreHeartRegister;
        private readonly FallbackDataProvider dataProvider;

        public CharacterDataFinalizer(
            IModLogger<CharacterDataFinalizer> logger,
            ICache<IDefinition<CharacterData>> cache,
            IRegister<AssetReferenceGameObject> assetReferenceRegister,
            IRegister<GameObject> gameObjectRegister,
            IRegister<CharacterTriggerData> triggerRegister,
            IRegister<VfxAtLoc> vfxRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<CardData> cardRegister,
            IRegister<CharacterChatterData> chatterRegister,
            IRegister<SubtypeData> subtypeRegister,
            IRegister<RoomModifierData> roomModifierRegister,
            IRegister<RelicData> relicRegister,
            IRegister<SoundCueDefinition> soundCueRegister,
            IRegister<PyreHeartData> pyreHeartRegister,
            FallbackDataProvider dataProvider
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.assetReferenceRegister = assetReferenceRegister;
            this.gameObjectRegister = gameObjectRegister;
            this.triggerRegister = triggerRegister;
            this.vfxRegister = vfxRegister;
            this.statusRegister = statusRegister;
            this.cardRegister = cardRegister;
            this.chatterRegister = chatterRegister;
            this.subtypeRegister = subtypeRegister;
            this.roomModifierRegister = roomModifierRegister;
            this.relicRegister = relicRegister;
            this.soundCueRegister = soundCueRegister;
            this.pyreHeartRegister = pyreHeartRegister;
            this.dataProvider = dataProvider;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCharacterData((definition as CharacterDataDefinition)!);
            }
            cache.Clear();
        }

        public void FinalizeCharacterData(CharacterDataDefinition definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var copyData = definition.CopyData;
            var key = definition.Key;
            var overrideMode = definition.Override;

            logger.Log(LogLevel.Info, $"Finalizing Character {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            //handle art
            // May not be set to null via override
            var characterArtReference = configuration.GetSection("character_art").ParseReference();
            var assetReferencedGameObject = AccessTools.Field(typeof(CharacterData), "characterPrefabVariantRef").GetValue(copyData) as AssetReferenceGameObject;
            if (characterArtReference != null)
            {
                if (
                    assetReferenceRegister.TryLookupId(
                        characterArtReference.ToId(key, TemplateConstants.GameObject),
                        out var gameObject,
                        out var _,
                        characterArtReference.context
                    )
                )
                {
                    assetReferencedGameObject = gameObject;
                }
            }
            AccessTools.Field(typeof(CharacterData), "characterPrefabVariantRef").SetValue(data, assetReferencedGameObject);

            var soundEffects = configuration.GetSection("sound_effects").GetChildren().Select(x => x.ParseReference()).Where(x => x != null).Cast<ReferencedObject>();
            if (soundEffects.Any() && characterArtReference != null)
            {
                if (gameObjectRegister.TryLookupName(characterArtReference.ToId(key, TemplateConstants.GameObject), out var gameObject, out var _, characterArtReference.context))
                {
                    var holder = gameObject.AddComponent<CoreSoundEffectHolder>();
                    holder.SoundEffectData = ScriptableObject.CreateInstance<CoreSoundEffectData>();
                    List<SoundCueDefinition> sounds = [];
                    foreach (var soundReference in soundEffects)
                    {
                        if (soundCueRegister.TryLookupId(soundReference.ToId(key, TemplateConstants.SoundCueDefinition), out var sound, out var _2, soundReference.context))
                        {
                            sounds.Add(sound);
                        }
                    }
                    holder.SoundEffectData.Sounds = sounds.ToArray();
                }
            }

            //handle ability
            var abilityConfig = configuration.GetSection("ability");
            AccessTools.Field(typeof(CharacterData), "unitAbility").SetValue(data, copyData.GetUnitAbilityCardData());
            var abilityReference = abilityConfig.ParseReference();
            if (abilityReference != null)
            {
                cardRegister.TryLookupName(abilityReference.ToId(key, TemplateConstants.Card), out var abilityCard, out var _, abilityReference.context);
                if (abilityCard != null && !abilityCard.IsUnitAbility())
                {
                    logger.Log(LogLevel.Warning, $"Attempting to add {abilityCard.name} as a unit ability upgrade for {data.name}, but is not a unit ability card.");
                }
                AccessTools.Field(typeof(CharacterData), "unitAbility").SetValue(data, abilityCard);
            }
            if (overrideMode == OverrideMode.Replace && abilityReference == null && abilityConfig.Exists())
            {
                AccessTools.Field(typeof(CharacterData), "unitAbility").SetValue(data, null);
            }

            //handle equipment
            var graftedEquipmentConfig = configuration.GetSection("grafted_equipment");
            AccessTools.Field(typeof(CharacterData), "graftedEquipment").SetValue(data, copyData.GetGraftedEquipment());
            var graftedEquipmentCardReference = graftedEquipmentConfig.ParseReference();
            if (graftedEquipmentCardReference != null)
            {
                cardRegister.TryLookupName(graftedEquipmentCardReference.ToId(key, TemplateConstants.Card), out var equipmentCard, out var _, graftedEquipmentCardReference.context);
                AccessTools.Field(typeof(CharacterData), "graftedEquipment").SetValue(data, equipmentCard);
            }
            if (overrideMode == OverrideMode.Replace && graftedEquipmentConfig.Exists() && graftedEquipmentCardReference == null)
            {
                AccessTools.Field(typeof(CharacterData), "graftedEquipment").SetValue(data, null);
            }

            // Do not allow override to set to null. These need to be set to an empty VfxAtLoc
            var projectilePrefabReference = configuration.GetSection("projectile_vfx").ParseReference();
            AccessTools.Field(typeof(CharacterData), "projectilePrefab").SetValue(data, copyData.GetProjectilePrefab());
            if (overrideMode.IsNewContent() || projectilePrefabReference != null)
            {
                vfxRegister.TryLookupId(projectilePrefabReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var projectile_vfx, out var _, projectilePrefabReference?.context);
                AccessTools
                    .Field(typeof(CharacterData), "projectilePrefab")
                    .SetValue(data, projectile_vfx);
            }

            var attackVFXReference = configuration.GetSection("attack_vfx").ParseReference();
            AccessTools.Field(typeof(CharacterData), "attackVFX").SetValue(data, copyData.GetAttackVfx());
            if (overrideMode.IsNewContent() || attackVFXReference != null)
            {
                vfxRegister.TryLookupId(attackVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var attack_vfx, out var _, attackVFXReference?.context);
                AccessTools.Field(typeof(CharacterData), "attackVFX").SetValue(data, attack_vfx);
            }

            var impactVFXReference = configuration.GetSection("impact_vfx").ParseReference();
            AccessTools.Field(typeof(CharacterData), "impactVFX").SetValue(data, copyData.GetImpactVfx());
            if (overrideMode.IsNewContent() || impactVFXReference != null)
            {
                vfxRegister.TryLookupId(impactVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var impact_vfx, out var _, impactVFXReference?.context);
                AccessTools.Field(typeof(CharacterData), "impactVFX").SetValue(data, impact_vfx);
            }

            var deathVFXReference = configuration.GetSection("death_vfx").ParseReference();
            AccessTools.Field(typeof(CharacterData), "deathVFX").SetValue(data, copyData.GetDeathVfx());
            if (overrideMode.IsNewContent() || deathVFXReference != null)
            {
                vfxRegister.TryLookupId(deathVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var death_vfx, out var _, deathVFXReference?.context);
                AccessTools.Field(typeof(CharacterData), "deathVFX").SetValue(data, death_vfx);
            }

            var bossSpellCastVFXReference = configuration.GetDeprecatedSection("boss_cast_vfx", "boss_spell_cast_vfx").ParseReference();
            AccessTools.Field(typeof(CharacterData), "bossSpellCastVFX").SetValue(data, copyData.GetBossSpellCastVfx());
            if (overrideMode.IsNewContent() || bossSpellCastVFXReference != null)
            {
                vfxRegister.TryLookupId(bossSpellCastVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var boss_cast_vfx, out var _, bossSpellCastVFXReference?.context);
                AccessTools
                    .Field(typeof(CharacterData), "bossSpellCastVFX")
                    .SetValue(data, boss_cast_vfx);
            }

            var bossRoomSpellCastVFXReference = configuration.GetSection("boss_room_cast_vfx").ParseReference();
            AccessTools.Field(typeof(CharacterData), "bossRoomSpellCastVFX").SetValue(data, copyData.GetBossRoomSpellCastVfx());
            if (overrideMode.IsNewContent() || bossRoomSpellCastVFXReference != null)
            {
                vfxRegister.TryLookupId(bossRoomSpellCastVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "", out var boss_room_cast_vfx, out var _, bossRoomSpellCastVFXReference?.context);
                AccessTools
                    .Field(typeof(CharacterData), "bossRoomSpellCastVFX")
                    .SetValue(data, boss_room_cast_vfx);
            }

            //handle triggers
            var triggerDatas = copyData.GetTriggers().ToList() ?? [];
            if (copyData != data)
                triggerDatas = [.. triggerDatas];
            var triggerConfig = configuration.GetSection("triggers");
            if (overrideMode == OverrideMode.Replace && triggerConfig.Exists())
            {
                triggerDatas.Clear();
            }
            var triggerReferences = triggerConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in triggerReferences)
            {
                if (
                    triggerRegister.TryLookupId(
                        reference.ToId(key, TemplateConstants.CharacterTrigger),
                        out var trigger,
                        out var _,
                        reference.context
                    )
                )
                {
                    triggerDatas.Add(trigger);
                }
            }
            AccessTools.Field(typeof(CharacterData), "triggers").SetValue(data, triggerDatas);

            //status effect immunities
            var statusEffectImmunities = copyData.GetStatusEffectImmunities()?.ToList() ?? [];
            if (copyData != data)
                statusEffectImmunities = [.. statusEffectImmunities];
            var statusImmunityConfig = configuration.GetSection("status_effect_immunities");
            if (overrideMode == OverrideMode.Replace && statusImmunityConfig.Exists())
            {
                statusEffectImmunities.Clear();
            }
            var statusImmunityReferences = statusImmunityConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in statusImmunityReferences)
            {
                var statusEffectId = reference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, reference.context))
                {
                    statusEffectImmunities.Add(statusEffectData.GetStatusId());
                }
            }
            AccessTools
                .Field(typeof(CharacterData), "statusEffectImmunities")
                .SetValue(data, statusEffectImmunities.ToArray());

            //status
            var startingStatusEffects = copyData.GetStartingStatusEffects()?.ToList() ?? [];
            if (copyData != data)
                startingStatusEffects = [.. startingStatusEffects];
            var startingStatusEffectConfig = configuration.GetSection("starting_status_effects");
            if (overrideMode == OverrideMode.Replace && startingStatusEffectConfig.Exists())
            {
                startingStatusEffects.Clear();
            }
            foreach (var child in startingStatusEffectConfig.GetChildren())
            {
                var reference = child.GetSection("status").ParseReference();
                if (reference == null)
                    continue;
                var statusEffectId = reference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, reference.context))
                {
                    startingStatusEffects.Add(new StatusEffectStackData()
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child.GetSection("count").ParseInt() ?? 0,
                        fromPermanentUpgrade = child.GetSection("from_permanent_upgrade").ParseBool() ?? false
                    });
                }
            }
            AccessTools
                .Field(typeof(CharacterData), "startingStatusEffects")
                .SetValue(data, startingStatusEffects.ToArray());

            // TODO checkOverride is not honored, should allow merging the existing data.
            var chatterConfig = configuration.GetSection("chatter");
            AccessTools.Field(typeof(CharacterData), "characterChatterData").SetValue(data, copyData.GetCharacterChatterData());
            var chatterReference = chatterConfig.ParseReference();
            if (chatterReference != null)
            {
                if (overrideMode == OverrideMode.Append)
                {
                    logger.Log(LogLevel.Warning, $"Requested Append override mode for Character {definition.Id} key {definition.Key}, but this isn't supported for CharacterChatterData, replacing the chatter with what is given.");
                }
                if (chatterRegister.TryLookupId(chatterReference.ToId(key, TemplateConstants.Chatter), out var lookup, out var _, chatterReference.context))
                {
                    AccessTools.Field(typeof(CharacterData), "characterChatterData").SetValue(data, lookup);
                }
            }
            if (overrideMode == OverrideMode.Append && chatterReference == null && chatterConfig.Exists())
            {
                AccessTools.Field(typeof(CharacterData), "characterChatterData").SetValue(data, null);
            }

            //subtypes
            var subtypes = (List<string>)AccessTools.Field(typeof(CharacterData), "subtypeKeys").GetValue(copyData);
            if (copyData != data)
                subtypes = [.. subtypes];
            var subtypeConfig = configuration.GetSection("subtypes");
            if (overrideMode == OverrideMode.Replace && subtypeConfig.Exists())
            {
                subtypes.Clear();
            }
            var subtypeReferences = subtypeConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in subtypeReferences)
            {
                if (subtypeRegister.TryLookupId(reference.ToId(key, TemplateConstants.Subtype), out var lookup, out var _, reference.context))
                {
                    subtypes.Add(lookup.Key);
                }
            }
            AccessTools.Field(typeof(CharacterData), "subtypeKeys").SetValue(data, subtypes);

            var roomModifiers = copyData.GetRoomModifiersData();
            if (copyData != data)
                roomModifiers = [.. roomModifiers];
            var roomModifierConfig = configuration.GetSection("room_modifiers");
            if (overrideMode == OverrideMode.Replace && roomModifierConfig.Exists())
            {
                roomModifiers.Clear();
            }
            var roomModifierReferences = roomModifierConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in roomModifierReferences)
            {
                if (roomModifierRegister.TryLookupId(reference.ToId(key, TemplateConstants.RoomModifier), out var roomModifierData, out var _, reference.context))
                {
                    roomModifiers.Add(roomModifierData);
                }
            }
            AccessTools
                .Field(typeof(CharacterData), "roomModifiers")
                .SetValue(data, roomModifiers);

            var relicConfig = configuration.GetDeprecatedSection("enemy_relic_data", "enemy_relic");
            AccessTools.Field(typeof(CharacterData), "enemyRelicData").SetValue(data, copyData.GetEnemyRelicData());
            var relicReference = relicConfig.ParseReference();
            if (relicReference != null)
            {
                relicRegister.TryLookupId(relicReference.ToId(key, TemplateConstants.RelicData), out var relic, out var _, relicReference.context);
                AccessTools.Field(typeof(CharacterData), "enemyRelicData").SetValue(data, relic);
            }
            if (overrideMode == OverrideMode.Replace && relicReference == null && relicConfig.Exists())
            {
                AccessTools.Field(typeof(CharacterData), "enemyRelicData").SetValue(data, null);
            }

            var pyreHeartConfig = configuration.GetSection("pyre_heart_data");
            AccessTools.Field(typeof(CharacterData), "pyreHeartData").SetValue(data, copyData.GetPyreHeartData());
            var pyreHeartReference = pyreHeartConfig.ParseReference();
            if (pyreHeartReference != null)
            {
                pyreHeartRegister.TryLookupName(pyreHeartReference.ToId(key, TemplateConstants.PyreHeart), out var pyreHeart, out var _, pyreHeartReference.context);
                AccessTools.Field(typeof(CharacterData), "pyreHeartData").SetValue(data, pyreHeart);
            }
            if (overrideMode == OverrideMode.Replace && pyreHeartReference == null && pyreHeartConfig.Exists())
            {
                AccessTools.Field(typeof(CharacterData), "pyreHeartData").SetValue(data, null);
            }

            AccessTools.Field(typeof(CharacterData), "bossActionGroups").SetValue(data, copyData.GetBossActionData());

            AccessTools
                .Field(typeof(CharacterData), "fallbackData")
                .SetValue(data, dataProvider.FallbackData);
        }
    }
}
