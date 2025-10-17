using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TrainworksReloaded.Base.Trigger;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class TrackedValueTypeRegister : Dictionary<string, CardStatistics.TrackedValueType>, IRegister<CardStatistics.TrackedValueType>
    {
        private static readonly Dictionary<string, CardStatistics.TrackedValueType> VanillaTrackedValueTypeToEnum = new()
        {
            ["subtype_in_deck"] = CardStatistics.TrackedValueType.SubtypeInDeck,
            ["subtype_in_discard_pile"] = CardStatistics.TrackedValueType.SubtypeInDiscardPile,
            ["subtype_in_exhaust_pile"] = CardStatistics.TrackedValueType.SubtypeInExhaustPile,
            ["subtype_in_draw_pile"] = CardStatistics.TrackedValueType.SubtypeInDrawPile,
            ["subtype_in_eaten_pile"] = CardStatistics.TrackedValueType.SubtypeInEatenPile,
            ["type_in_deck"] = CardStatistics.TrackedValueType.TypeInDeck,
            ["type_in_discard_pile"] = CardStatistics.TrackedValueType.TypeInDiscardPile,
            ["type_in_exhaust_pile"] = CardStatistics.TrackedValueType.TypeInExhaustPile,
            ["type_in_draw_pile"] = CardStatistics.TrackedValueType.TypeInDrawPile,
            ["type_in_eaten_pile"] = CardStatistics.TrackedValueType.TypeInEatenPile,
            ["played_cost"] = CardStatistics.TrackedValueType.PlayedCost,
            ["unmodified_played_cost"] = CardStatistics.TrackedValueType.UnmodifiedPlayedCost,
            ["heroes_killed"] = CardStatistics.TrackedValueType.HeroesKilled,
            ["spawned_monster_deaths"] = CardStatistics.TrackedValueType.SpawnedMonsterDeaths,
            ["times_discarded"] = CardStatistics.TrackedValueType.TimesDiscarded,
            ["times_played"] = CardStatistics.TrackedValueType.TimesPlayed,
            ["times_drawn"] = CardStatistics.TrackedValueType.TimesDrawn,
            ["times_exhausted"] = CardStatistics.TrackedValueType.TimesExhausted,
            ["last_sacrificed_monster_stats"] = CardStatistics.TrackedValueType.LastSacrificedMonsterStats,
            ["any_hero_killed"] = CardStatistics.TrackedValueType.AnyHeroKilled,
            ["any_monster_death"] = CardStatistics.TrackedValueType.AnyMonsterDeath,
            ["any_monster_spawned"] = CardStatistics.TrackedValueType.AnyMonsterSpawned,
            ["any_discarded"] = CardStatistics.TrackedValueType.AnyDiscarded,
            ["any_card_played"] = CardStatistics.TrackedValueType.AnyCardPlayed,
            ["any_card_drawn"] = CardStatistics.TrackedValueType.AnyCardDrawn,
            ["any_exhausted"] = CardStatistics.TrackedValueType.AnyExhausted,
            ["any_character"] = CardStatistics.TrackedValueType.AnyCharacter,
            ["any_monster_spawned_top_floor"] = CardStatistics.TrackedValueType.AnyMonsterSpawnedTopFloor,
            ["monster_subtype_played"] = CardStatistics.TrackedValueType.MonsterSubtypePlayed,
            ["status_effect_count_in_target_room"] = CardStatistics.TrackedValueType.StatusEffectCountInTargetRoom,
            ["corruption_in_target_room"] = CardStatistics.TrackedValueType.CorruptionInTargetRoom,
            ["turn_count"] = CardStatistics.TrackedValueType.TurnCount,
            ["dragons_hoard_amount"] = CardStatistics.TrackedValueType.DragonsHoardAmount,
            ["moon_phase"] = CardStatistics.TrackedValueType.MoonPhase,
            ["magic_power_in_target_room"] = CardStatistics.TrackedValueType.MagicPowerInTargetRoom,
            ["gold"] = CardStatistics.TrackedValueType.Gold,
            ["status_effect_count_on_last_ability_activator"] = CardStatistics.TrackedValueType.StatusEffectCountOnLastAbilityActivator,
            ["pyre_heart_resurrection"] = CardStatistics.TrackedValueType.PyreHeartResurrection,
            ["num_specific_cards_in_deck"] = CardStatistics.TrackedValueType.NumSpecificCardsInDeck,
            ["any_status_effect_stacks_added"] = CardStatistics.TrackedValueType.AnyStatusEffectStacksAdded,
            ["any_status_effect_stacks_removed"] = CardStatistics.TrackedValueType.AnyStatusEffectStacksRemoved,
            ["last_attack_damage_dealt"] = CardStatistics.TrackedValueType.LastAttackDamageDealt,
        };

        private readonly IModLogger<TrackedValueTypeRegister> logger;

        public TrackedValueTypeRegister(IModLogger<TrackedValueTypeRegister> logger)
        {
            this.logger = logger;
            this.AddRange(VanillaTrackedValueTypeToEnum);
        }

        List<string> IRegister<CardStatistics.TrackedValueType>.GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => []
            };
        }

        void IRegisterableDictionary<CardStatistics.TrackedValueType>.Register(string key, CardStatistics.TrackedValueType item)
        {
            logger.Log(LogLevel.Info, $"Register TrackedValueType Enum ({key})");
            Add(key, item);
        }

        bool IRegister<CardStatistics.TrackedValueType>.TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CardStatistics.TrackedValueType lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaTrackedValueTypeToEnum.ContainsKey(identifier);
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
