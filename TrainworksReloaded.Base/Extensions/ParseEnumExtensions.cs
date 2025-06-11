using HarmonyLib;
using Microsoft.Extensions.Configuration;
using ShinyShoe;
using System;
using System.Linq;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using UnityEngine;
using static BossState;
using static CardEffectData;
using static CardStatistics;
using static CharacterTriggerData;
using static CharacterUI;
using static MetagameSaveData;
using static TooltipDesigner;
using static VfxAtLoc;
using static RelicEffectCondition;

namespace TrainworksReloaded.Base.Extensions
{
    public static class ParseEnumExtensions
    {
        public static OverrideMode ParseOverrideMode(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return OverrideMode.New;
            }
            return val.ToLower() switch
            {
                "false" => OverrideMode.New,
                "true" => OverrideMode.Replace,
                "replace" => OverrideMode.Replace,
                "append" => OverrideMode.Append,
                "clone" => OverrideMode.Clone,
                _ => OverrideMode.New
            };
        }

        public static MapNodeData.SkipCheckSettings? ParseSkipSettings(
            this IConfigurationSection section
        )
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "none" => 0,
                "always" => MapNodeData.SkipCheckSettings.Always,
                "if_full_health" => MapNodeData.SkipCheckSettings.IfFullHealth,
                "both" => MapNodeData.SkipCheckSettings.Always
                    | MapNodeData.SkipCheckSettings.IfFullHealth,
                _ => null,
            };
        }

        public static RewardData.Filter? ParseRewardFilter(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.ToLower();
            var values = val.Split('|', StringSplitOptions.RemoveEmptyEntries)
                           .Select(v => v.Trim())
                           .ToList();

            RewardData.Filter result = RewardData.Filter.None;
            foreach (var value in values)
            {
                RewardData.Filter? flag = value switch
                {
                    "none" => RewardData.Filter.None,
                    "only_endless" => RewardData.Filter.OnlyInEndless,
                    "not_endless" => RewardData.Filter.NotInEndless,
                    "only_if_allied_champ" => RewardData.Filter.OnlyIfHasAlliedChampion,
                    _ => null
                };

                if (flag == null)
                {
                    return null;
                }

                result |= flag.Value;
            }

            return result;
        }

        public static CardData.CostType? ParseCostType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "default" => CardData.CostType.Default,
                "x" => CardData.CostType.ConsumeRemainingEnergy,
                "unplayable" => CardData.CostType.NonPlayable,
                _ => null,
            };
        }

        public static CardType? ParseCardType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "invalid" => CardType.Invalid,
                "spell" => CardType.Spell,
                "monster" => CardType.Monster,
                "blight" => CardType.Blight,
                "junk" => CardType.Junk,
                "equipment" => CardType.Equipment,
                "room" => CardType.TrainRoomAttachment,
                _ => null,
            };
        }

        public static CollectableRarity? ParseRarity(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "common" => CollectableRarity.Common,
                "uncommon" => CollectableRarity.Uncommon,
                "starter" => CollectableRarity.Starter,
                "rare" => CollectableRarity.Rare,
                "champion" => CollectableRarity.Champion,
                "unset" => CollectableRarity.Unset,
                _ => null,
            };
        }

        public static DLC? ParseDLC(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "none" => DLC.None,
                _ => null,
            };
        }

        public static CardInitialKeyboardTarget? ParseKeyboardTarget(
            this IConfigurationSection section
        )
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "front_friendly" => CardInitialKeyboardTarget.FrontFriendly,
                "front_enemy" => CardInitialKeyboardTarget.FrontEnemy,
                "back_friendly" => CardInitialKeyboardTarget.BackFriendly,
                "back_enemy" => CardInitialKeyboardTarget.BackEnemy,
                _ => null,
            };
        }

        public static Color? ParseColor(this IConfigurationSection section)
        {
            var r = section.GetSection("r").ParseFloat();
            var g = section.GetSection("g").ParseFloat();
            var b = section.GetSection("b").ParseFloat();
            var a = section.GetSection("a").ParseFloat();
            if (r == null && g == null && b == null && a == null)
            {
                return null;
            }
            return new Color(r ?? 0.0f, g ?? 0.0f, b ?? 0.0f, a ?? 1.0f);
        }

        public static string? ParseLocalization(this IConfigurationSection section)
        {
            var str = section.Value;
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            // Quote string if it contains quotes otherwise it will break when sending to I2.Loc
            if (str.Contains(','))
            {
                str = string.Format("\"{0}\"", str);
            }
            return str;
        }

        public static LocalizationTerm? ParseLocalizationTerm(this IConfigurationSection section)
        {
            var key = section.GetSection("id").Value;
            var type = section.GetSection("type").Value;
            var description = section.GetSection("description").Value;
            var group = section.GetSection("group").Value;
            var speaker_descriptions = section.GetSection("speaker_descriptions").Value;
            var english = section.GetSection("english").ParseLocalization();
            var french = section.GetSection("french").ParseLocalization();
            var german = section.GetSection("german").ParseLocalization();
            var russian = section.GetSection("russian").ParseLocalization();
            var portuguese = section.GetSection("portuguese").ParseLocalization();
            var chinese = section.GetSection("chinese").ParseLocalization();
            var spanish = section.GetSection("spanish").ParseLocalization();
            var chinese_traditional = section.GetSection("chinese_traditional").ParseLocalization();
            var korean = section.GetSection("korean").ParseLocalization();
            var japanese = section.GetSection("japanese").ParseLocalization();
            if (
                key == null
                && type == null
                && description == null
                && group == null
                && speaker_descriptions == null
                && english == null
                && french == null
                && german == null
                && russian == null
                && portuguese == null
                && chinese == null
                && spanish == null
                && chinese_traditional == null
                && korean == null
                && japanese == null
            )
            {
                return null;
            }

            // I don't get the nullability stuff moving the ?? "" to the english line still makes it
            // think it could be a null value even if defined as a string explicitly.
            string def = english ?? "";

            return new LocalizationTerm()
            {
                Key = key ?? "",
                Type = type ?? "Text",
                Desc = description ?? "",
                Group = group ?? "",
                Descriptions = speaker_descriptions ?? "",
                English = def,
                French = french ?? def,
                German = german ?? def,
                Russian = russian ?? def,
                Portuguese = portuguese ?? def,
                Chinese = chinese ?? def,
                Spanish = spanish ?? def,
                ChineseTraditional = chinese_traditional ?? def,
                Korean = korean ?? def,
                Japanese = japanese ?? def,
            };
        }

        public static ClassCardStyle? ParseCardStyle(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "none" => ClassCardStyle.None,
                "banished" => ClassCardStyle.Banished,
                "pyreborne" => ClassCardStyle.Pyreborne,
                _ => null,
            };
        }

        private static CardTargetMode? DoParseCardTargetMode(string? val)
        {
            if (val == null)
                return null;
            return val.ToLower() switch
            {
                "none" => CardTargetMode.NONE,
                "all" => CardTargetMode.All,
                "single" => CardTargetMode.SingleTarget,
                "targetless" => CardTargetMode.Targetless,
                "other" => CardTargetMode.Other,
                _ => null
            };
        }

        public static CardTargetMode? ParseCardTargetMode(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                if (!section.GetChildren().Any())
                {
                    return null;
                }
                CardTargetMode ret = CardTargetMode.All;
                foreach (var child in section.GetChildren())
                {
                    var flag = DoParseCardTargetMode(child.Value);
                    if (flag == null)
                        return null;
                    ret |= flag.Value;
                }
                return ret;
            }
            else
            {
                var values = val.Split('|', StringSplitOptions.RemoveEmptyEntries)
                               .Select(v => v.Trim())
                               .ToList();

                CardTargetMode result = CardTargetMode.All;
                foreach (var value in values)
                {
                    var flag = DoParseCardTargetMode(value);
                    if (flag == null)
                        return null;
                    result |= flag.Value;
                }
                return result;
            }

        }

        public static object ParseCompareOperator(this IConfigurationSection section, string defaultVal = "and")
        {
            Type realEnumType = AccessTools.Inner(typeof(CardUpgradeMaskData), "CompareOperator");
            var val = section.Value?.ToLower() ?? defaultVal;
            if (!System.Enum.TryParse(realEnumType, val, ignoreCase: true, out var enumType))
            {
                System.Enum.TryParse(realEnumType, defaultVal, ignoreCase: true, out enumType);
            }
            return enumType;
        }

        public static TrackedValue? ParseTrackedValue(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val switch
            {
                "none" => TrackedValue.None,
                "kill_enemies" => TrackedValue.KillEnemies,
                "play_spells" => TrackedValue.PlaySpells,
                "play_units" => TrackedValue.PlayUnits,
                "started_runs" => TrackedValue.StartedRuns,
                "covenant_level" => TrackedValue.CovenantLevel,
                "unlocked_pyre_hearts" => TrackedValue.UnlockedPyreHearts,
                "equipment_used" => TrackedValue.EquipmentUsed,
                "defeated_true_final_boss" => TrackedValue.DefeatedTrueFinalBoss,
                "cards_discarded" => TrackedValue.CardsDiscarded,
                "cards_consumed" => TrackedValue.CardsConsumed,
                "healing_done_after_crew_unlocked" => TrackedValue.HealingDoneAfterCrewUnlocked,
                "units_resurrected_after_crew_unlocked" =>
                    TrackedValue.UnitsResurrectedAfterCrewUnlocked,
                "consume_spells_returned_after_crew_unlocked" =>
                    TrackedValue.ConsumeSpellsReturnedAfterCrewUnlocked,
                "units_moved" => TrackedValue.UnitsMoved,
                "pyre_damage" => TrackedValue.PyreDamage,
                "debuffs_applied" => TrackedValue.DebuffsApplied,
                "enemies_defeated_with_spells" => TrackedValue.EnemiesDefeatedWithSpells,
                "battles_won_with_trial" => TrackedValue.BattlesWonWithTrial,
                "magic_power_bonus_damage" => TrackedValue.MagicPowerBonusDamage,
                "play_five_spells_in_turn_after_crew_unlocked" =>
                    TrackedValue.PlayFiveSpellsInTurnAfterCrewUnlocked,
                "play_dragon_or_demon_units" => TrackedValue.PlayDragonOrDemonUnits,
                "damage_done_by_train_stewards" => TrackedValue.DamageDoneByTrainStewards,
                "num_challenges_won" => TrackedValue.NumChallengesWon,
                "defeated_all_seraph_variants" => TrackedValue.DefeatedAllSeraphVariants,
                "defeated_all_seraph_variants_at_max_covenant" =>
                    TrackedValue.DefeatedAllSeraphVariantsAtMaxCovenant,
                "win_with_all_class_combos" => TrackedValue.WinWithAllClassCombos,
                "win_with_all_class_combos_at_max_covenant" =>
                    TrackedValue.WinWithAllClassCombosAtMaxCovenant,
                "all_classes_at_max_level" => TrackedValue.AllClassesAtMaxLevel,
                "win_with_all_classes_at_max_covenant" =>
                    TrackedValue.WinWithAllClassesAtMaxCovenant,
                "cards_frozen" => TrackedValue.CardsFrozen,
                "coins_spents" => TrackedValue.CoinsSpents,
                "room_capacity_increased_after_crew_unlocked" =>
                    TrackedValue.RoomCapacityIncreasedAfterCrewUnlocked,
                "defeat_tfb_with_all_class_combos_at_max_covenant" =>
                    TrackedValue.DefeatTfbWithAllClassCombosAtMaxCovenant,
                _ => null,
            };
        }

        public static CardStatistics.TrackedValueType? ParseTrackedValueType(
            this IConfigurationSection section
        )
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "subtype_in_deck" => TrackedValueType.SubtypeInDeck,
                "subtype_in_discard_pile" => TrackedValueType.SubtypeInDiscardPile,
                "subtype_in_exhaust_pile" => TrackedValueType.SubtypeInExhaustPile,
                "subtype_in_draw_pile" => TrackedValueType.SubtypeInDrawPile,
                "subtype_in_eaten_pile" => TrackedValueType.SubtypeInEatenPile,
                "type_in_deck" => TrackedValueType.TypeInDeck,
                "type_in_discard_pile" => TrackedValueType.TypeInDiscardPile,
                "type_in_exhaust_pile" => TrackedValueType.TypeInExhaustPile,
                "type_in_draw_pile" => TrackedValueType.TypeInDrawPile,
                "type_in_eaten_pile" => TrackedValueType.TypeInEatenPile,
                "played_cost" => TrackedValueType.PlayedCost,
                "unmodified_played_cost" => TrackedValueType.UnmodifiedPlayedCost,
                "heroes_killed" => TrackedValueType.HeroesKilled,
                "spawned_monster_deaths" => TrackedValueType.SpawnedMonsterDeaths,
                "times_discarded" => TrackedValueType.TimesDiscarded,
                "times_played" => TrackedValueType.TimesPlayed,
                "times_drawn" => TrackedValueType.TimesDrawn,
                "times_exhausted" => TrackedValueType.TimesExhausted,
                "last_sacrificed_monster_stats" => TrackedValueType.LastSacrificedMonsterStats,
                "any_hero_killed" => TrackedValueType.AnyHeroKilled,
                "any_monster_death" => TrackedValueType.AnyMonsterDeath,
                "any_monster_spawned" => TrackedValueType.AnyMonsterSpawned,
                "any_discarded" => TrackedValueType.AnyDiscarded,
                "any_card_played" => TrackedValueType.AnyCardPlayed,
                "any_card_drawn" => TrackedValueType.AnyCardDrawn,
                "any_exhausted" => TrackedValueType.AnyExhausted,
                "any_monster_spawned_top_floor" => TrackedValueType.AnyMonsterSpawnedTopFloor,
                "monster_subtype_played" => TrackedValueType.MonsterSubtypePlayed,
                "status_effect_count_in_target_room" =>
                    TrackedValueType.StatusEffectCountInTargetRoom,
                "corruption_in_target_room" => TrackedValueType.CorruptionInTargetRoom,
                "turn_count" => TrackedValueType.TurnCount,
                "dragons_hoard_amount" => TrackedValueType.DragonsHoardAmount,
                "moon_phase" => TrackedValueType.MoonPhase,
                "magic_power_in_target_room" => TrackedValueType.MagicPowerInTargetRoom,
                "gold" => TrackedValueType.Gold,
                "status_effect_count_on_last_ability_activator" =>
                    TrackedValueType.StatusEffectCountOnLastAbilityActivator,
                "const_one" => TrackedValueType.ConstOne,
                "pyre_heart_resurrection" => TrackedValueType.PyreHeartResurrection,
                "num_specific_cards_in_deck" => TrackedValueType.NumSpecificCardsInDeck,
                "any_status_effect_stacks_added" => TrackedValueType.AnyStatusEffectStacksAdded,
                "any_status_effect_stacks_removed" => TrackedValueType.AnyStatusEffectStacksRemoved,
                "last_attack_damage_dealt" => TrackedValueType.LastAttackDamageDealt,
                _ => null,
            };
        }

        public static CardStatistics.CardTypeTarget? ParseCardTypeTarget(
            this IConfigurationSection section
        )
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "any" => CardTypeTarget.Any,
                "spell" => CardTypeTarget.Spell,
                "monster" => CardTypeTarget.Monster,
                "blight" => CardTypeTarget.Blight,
                "junk" => CardTypeTarget.Junk,
                "equipment" => CardTypeTarget.Equipment,
                "train_room_attachment" => CardTypeTarget.TrainRoomAttachment,
                _ => null,
            };
        }

        public static CardStatistics.EntryDuration? ParseEntryDuration(
            this IConfigurationSection section
        )
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "this_turn" => EntryDuration.ThisTurn,
                "this_battle" => EntryDuration.ThisBattle,
                "previous_turn" => EntryDuration.PreviousTurn,
                _ => null,
            };
        }

        public static Team.Type? ParseTeamType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            return val.ToLower() switch
            {
                "none" => Team.Type.None,
                "heroes" => Team.Type.Heroes,
                "monsters" => Team.Type.Monsters,
                "both" => Team.Type.Heroes | Team.Type.Monsters,
                _ => null,
            };
        }

        public static CardTraitData.StackMode? ParseStackMode(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "none" => CardTraitData.StackMode.None,
                "param_int" => CardTraitData.StackMode.ParamInt,
                "param_int_largest" => CardTraitData.StackMode.ParamIntLargest,
                _ => null,
            };
        }

        public static TargetMode? ParseTargetMode(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "room" => TargetMode.Room,
                "random_in_room" => TargetMode.RandomInRoom,
                "front_in_room" => TargetMode.FrontInRoom,
                "room_heal_targets" => TargetMode.RoomHealTargets,
                "self" => TargetMode.Self,
                "last_attacked_character" => TargetMode.LastAttackedCharacter,
                "front_with_status" => TargetMode.FrontWithStatus,
                "tower" => TargetMode.Tower,
                "back_in_room" => TargetMode.BackInRoom,
                "drop_target_character" => TargetMode.DropTargetCharacter,
                "draw_pile" => TargetMode.DrawPile,
                "discard" => TargetMode.Discard,
                "exhaust" => TargetMode.Exhaust,
                "eaten" => TargetMode.Eaten,
                "weakest" => TargetMode.Weakest,
                "last_attacker_character" => TargetMode.LastAttackerCharacter,
                "hand" => TargetMode.Hand,
                "last_drawn_card" => TargetMode.LastDrawnCard,
                "last_feeder_character" => TargetMode.LastFeederCharacter,
                "pyre" => TargetMode.Pyre,
                "last_targeted_characters" => TargetMode.LastTargetedCharacters,
                "last_damaged_characters" => TargetMode.LastDamagedCharacters,
                "last_spawned_morsel" => TargetMode.LastSpawnedMorsel,
                "front_in_all_rooms" => TargetMode.FrontInAllRooms,
                "front_in_room_and_room_above" => TargetMode.FrontInRoomAndRoomAbove,
                "weakest_all_rooms" => TargetMode.WeakestAllRooms,
                "strongest_all_rooms" => TargetMode.StrongestAllRooms,
                "last_equipped_character" => TargetMode.LastEquippedCharacter,
                "last_sacrificed_character" => TargetMode.LastSacrificedCharacter,
                _ => null,
            };
        }

        public static CardEffectData.HealthFilter? ParseHealthFilter(
            this IConfigurationSection section
        )
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "both" => CardEffectData.HealthFilter.Both,
                "undamaged" => CardEffectData.HealthFilter.Undamaged,
                "damaged" => CardEffectData.HealthFilter.Damaged,
                _ => null,
            };
        }

        public static CardSelectionMode? ParseCardSelectionMode(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "choose_to_hand" => CardSelectionMode.ChooseToHand,
                "random_to_hand" => CardSelectionMode.RandomToHand,
                "choose_to_deck" => CardSelectionMode.ChooseToDeck,
                "random_to_deck" => CardSelectionMode.RandomToDeck,
                "random_to_room" => CardSelectionMode.RandomToRoom,
                "random_to_hand_with_upgrades" => CardSelectionMode.RandomToHandWithUpgrades,
                "random_to_room_until_capacity_full" =>
                    CardSelectionMode.RandomToRoomUntilCapacityFull,
                _ => null,
            };
        }

        public static Anim? ParseAnim(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "none" => Anim.None,
                "idle" => Anim.Idle,
                "attack" => Anim.Attack,
                "hit_react" => Anim.HitReact,
                "idle_relentless" => Anim.Idle_Relentless,
                "attack_spell" => Anim.Attack_Spell,
                "death" => Anim.Death,
                "talk" => Anim.Talk,
                "hover" => Anim.Hover,
                _ => null,
            };
        }

        public static CharacterTriggerData.Trigger? ParseTrigger(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "on_death" => CharacterTriggerData.Trigger.OnDeath,
                "post_combat" => CharacterTriggerData.Trigger.PostCombat,
                "on_spawn" => CharacterTriggerData.Trigger.OnSpawn,
                "on_attacking" => CharacterTriggerData.Trigger.OnAttacking,
                "on_kill" => CharacterTriggerData.Trigger.OnKill,
                "on_any_hero_death_on_floor" => CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor,
                "on_any_monster_death_on_floor" => CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor,
                "on_heal" => CharacterTriggerData.Trigger.OnHeal,
                "on_team_turn_begin" => CharacterTriggerData.Trigger.OnTeamTurnBegin,
                "pre_combat" => CharacterTriggerData.Trigger.PreCombat,
                "post_ascension" => CharacterTriggerData.Trigger.PostAscension,
                "post_combat_healing" => CharacterTriggerData.Trigger.PostCombatHealing,
                "on_hit" => CharacterTriggerData.Trigger.OnHit,
                "after_spawn_enchant" => CharacterTriggerData.Trigger.AfterSpawnEnchant,
                "post_descension" => CharacterTriggerData.Trigger.PostDescension,
                "on_any_unit_death_on_floor" => CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor,
                "card_spell_played" => CharacterTriggerData.Trigger.CardSpellPlayed,
                "card_monster_played" => CharacterTriggerData.Trigger.CardMonsterPlayed,
                "end_turn_pre_hand_discard" => CharacterTriggerData.Trigger.EndTurnPreHandDiscard,
                "on_feed" => CharacterTriggerData.Trigger.OnFeed,
                "on_eaten" => CharacterTriggerData.Trigger.OnEaten,
                "on_turn_begin" => CharacterTriggerData.Trigger.OnTurnBegin,
                "on_burnout" => CharacterTriggerData.Trigger.OnBurnout,
                "on_spawn_not_from_card" => CharacterTriggerData.Trigger.OnSpawnNotFromCard,
                "on_unscaled_spawn" => CharacterTriggerData.Trigger.OnUnscaledSpawn,
                "post_attempted_ascension" => CharacterTriggerData.Trigger.PostAttemptedAscension,
                "post_attempted_descension" => CharacterTriggerData.Trigger.PostAttemptedDescension,
                "on_hatched" => CharacterTriggerData.Trigger.OnHatched,
                "card_corrupt_played" => CharacterTriggerData.Trigger.CardCorruptPlayed,
                "corruption_added" => CharacterTriggerData.Trigger.CorruptionAdded,
                "on_armor_added" => CharacterTriggerData.Trigger.OnArmorAdded,
                "on_food_spawn" => CharacterTriggerData.Trigger.OnFoodSpawn,
                "card_exhausted" => CharacterTriggerData.Trigger.CardExhausted,
                "on_remove_hatch" => CharacterTriggerData.Trigger.OnRemoveHatch,
                "on_own_ability_activated" => CharacterTriggerData.Trigger.OnOwnAbilityActivated,
                "card_regal_played" => CharacterTriggerData.Trigger.CardRegalPlayed,
                "regal_count_added" => CharacterTriggerData.Trigger.RegalCountAdded,
                "on_unit_ability_available" => CharacterTriggerData.Trigger.OnUnitAbilityAvailable,
                "on_unit_ability_unavailable" => CharacterTriggerData.Trigger.OnUnitAbilityUnavailable,
                "on_shift" => CharacterTriggerData.Trigger.OnShift,
                "on_equipment_added" => CharacterTriggerData.Trigger.OnEquipmentAdded,
                "on_equipment_removed" => CharacterTriggerData.Trigger.OnEquipmentRemoved,
                "on_deathwish" => CharacterTriggerData.Trigger.OnDeathwish,
                "on_deathwish_lost" => CharacterTriggerData.Trigger.OnDeathwishLost,
                "on_valiant" => CharacterTriggerData.Trigger.OnValiant,
                "on_encounter_complete" => CharacterTriggerData.Trigger.OnEncounterComplete,
                "on_pyregel_added" => CharacterTriggerData.Trigger.OnPyregelAdded,
                "on_moon_phase_shift" => CharacterTriggerData.Trigger.OnMoonPhaseShift,
                "on_equipment_added_to_ally" => CharacterTriggerData.Trigger.OnEquipmentAddedToAlly,
                "on_timebomb" => CharacterTriggerData.Trigger.OnTimebomb,
                "on_reanimated" => CharacterTriggerData.Trigger.OnReanimated,
                "on_graft_equipment_added" => CharacterTriggerData.Trigger.OnGraftEquipmentAdded,
                "on_new_status_effect_added" => CharacterTriggerData.Trigger.OnNewStatusEffectAdded,
                "on_queued_status_effect_to_add" => CharacterTriggerData.Trigger.OnQueuedStatusEffectToAdd,
                "on_moon_lit" => CharacterTriggerData.Trigger.OnMoonLit,
                "on_moon_shade" => CharacterTriggerData.Trigger.OnMoonShade,
                "on_moonlit_lost" => CharacterTriggerData.Trigger.OnMoonlitLost,
                "on_moonshade_lost" => CharacterTriggerData.Trigger.OnMoonshadeLost,
                "on_troop_added" => CharacterTriggerData.Trigger.OnTroopAdded,
                "on_troop_removed" => CharacterTriggerData.Trigger.OnTroopRemoved,
                "on_train_room_loop" => CharacterTriggerData.Trigger.OnTrainRoomLoop,
                "on_status_effect_changed" => CharacterTriggerData.Trigger.OnStatusEffectChanged,
                "on_attacking_before_damage" => CharacterTriggerData.Trigger.OnAttackingBeforeDamage,
                "on_silence" => CharacterTriggerData.Trigger.OnSilence,
                "on_silence_lost" => CharacterTriggerData.Trigger.OnSilenceLost,
                _ => null,
            };
        }

        public static AttackPhase? ParseAttackPhase(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "none" => 0, // No flags set
                "casting" => AttackPhase.Casting,
                "relentless" => AttackPhase.Relentless,
                "both" => AttackPhase.Casting | AttackPhase.Relentless, // Both flags set
                _ => null, // Invalid input
            };
        }

        public static CharacterDeathVFX.Type? ParseCharacterDeathType(
            this IConfigurationSection section
        )
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "none" => CharacterDeathVFX.Type.NONE,
                "normal" => CharacterDeathVFX.Type.Normal,
                "large" => CharacterDeathVFX.Type.Large,
                "boss" => CharacterDeathVFX.Type.Boss,
                _ => null, // Invalid input
            };
        }

        public static TitanAffinity? ParseTitanAffinity(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            return val.ToLower() switch
            {
                "none" => TitanAffinity.None,
                "entropy" => TitanAffinity.Entropy,
                "savagery" => TitanAffinity.Savagery,
                "dominion" => TitanAffinity.Dominion,
                _ => null, // Invalid input
            };
        }

        public static CardTriggerType? ParseCardTriggerType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.ToLower();

            return val switch
            {
                "on_cast" => CardTriggerType.OnCast,
                "on_kill" => CardTriggerType.OnKill,
                "on_discard" => CardTriggerType.OnDiscard,
                "on_monster_death" => CardTriggerType.OnMonsterDeath,
                "on_any_monster_death_on_floor" => CardTriggerType.OnAnyMonsterDeathOnFloor,
                "on_any_hero_death_on_floor" => CardTriggerType.OnAnyHeroDeathOnFloor,
                "on_healed" => CardTriggerType.OnHealed,
                "on_player_damage_taken" => CardTriggerType.OnPlayerDamageTaken,
                "on_any_unit_death_on_floor" => CardTriggerType.OnAnyUnitDeathOnFloor,
                "on_treasure" => CardTriggerType.OnTreasure,
                "on_unplayed_negative" => CardTriggerType.OnUnplayedNegative,
                "on_feed" => CardTriggerType.OnFeed,
                "on_exhausted" => CardTriggerType.OnExhausted,
                "on_unplayed_positive" => CardTriggerType.OnUnplayedPositive,
                _ => null,
            };
        }

        public static PersistenceMode? ParsePersistenceMode(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.ToLower();

            return val switch
            {
                "single_run" => PersistenceMode.SingleRun,
                "single_battle" => PersistenceMode.SingleBattle,
                _ => null,
            };
        }

        public static VfxAtLoc.Location? ParseLocation(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.ToLower();

            return val switch
            {
                "none" => VfxAtLoc.Location.None,
                "room_center" => VfxAtLoc.Location.RoomCenter,
                "character_top" => VfxAtLoc.Location.CharacterTop,
                "character_center" => VfxAtLoc.Location.CharacterCenter,
                "character_bottom" => VfxAtLoc.Location.CharacterBottom,
                "character_side_fwd_center" => VfxAtLoc.Location.CharacterSideFwdCenter,
                "character_side_fwd_bottom" => VfxAtLoc.Location.CharacterSideFwdBottom,
                "character_side_back_center" => VfxAtLoc.Location.CharacterSideBackCenter,
                "character_side_back_bottom" => VfxAtLoc.Location.CharacterSideBackBottom,
                "bone_status_effect_slot1" => VfxAtLoc.Location.BoneStatusEffectSlot1,
                "bone_status_effect_slot2" => VfxAtLoc.Location.BoneStatusEffectSlot2,
                "bone_status_effect_slot3" => VfxAtLoc.Location.BoneStatusEffectSlot3,
                "bone_status_effect_slot4" => VfxAtLoc.Location.BoneStatusEffectSlot4,
                _ => null,
            };
        }

        public static Facing? ParseFacing(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.ToLower();

            return val switch
            {
                "none" => Facing.None,
                "forward" => Facing.Forward,
                _ => null,
            };
        }

        public static TooltipDesignType? ParseTooltipDesignType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.ToLower();

            return val switch
            {
                "default" => TooltipDesignType.Default,
                "lore_herzal" => TooltipDesignType.LoreHerzal,
                "boss" => TooltipDesignType.Boss,
                "default_wide" => TooltipDesignType.DefaultWide,
                "positive" => TooltipDesignType.Positive,
                "negative" => TooltipDesignType.Negative,
                "persistent" => TooltipDesignType.Persistent,
                "trigger" => TooltipDesignType.Trigger,
                "keyword" => TooltipDesignType.Keyword,
                "lore_malicka" => TooltipDesignType.LoreMalicka,
                "lore_heph" => TooltipDesignType.LoreHeph,
                "default_mega_wide" => TooltipDesignType.DefaultMegaWide,
                "state_modifier" => TooltipDesignType.StateModifier,
                "title" => TooltipDesignType.Title,
                "equipment" => TooltipDesignType.Equipment,
                "ability" => TooltipDesignType.Ability,
                "tip" => TooltipDesignType.Tip,
                "boss_title" => TooltipDesignType.BossTitle,
                "relic_title" => TooltipDesignType.RelicTitle,
                _ => null,
            };
        }

        public static StatusEffectData.DisplayCategory? ParseDisplayCategory(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            val = val.ToLower();
            return val switch
            {
                "positive" => StatusEffectData.DisplayCategory.Positive,
                "negative" => StatusEffectData.DisplayCategory.Negative,
                "persistent" => StatusEffectData.DisplayCategory.Persistent,
                "ability" => StatusEffectData.DisplayCategory.Ability,
                _ => null
            };
        }

        public static StatusEffectData.VFXDisplayType? ParseVFXDisplayType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            val = val.ToLower();
            return val switch
            {
                "default" => StatusEffectData.VFXDisplayType.Default,
                "last_stack" => StatusEffectData.VFXDisplayType.LastStack,
                _ => null
            };
        }

        public static StatusEffectData.TriggerStage? ParseTriggerStage(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            val = val.ToLower();
            return val switch
            {
                "none" => StatusEffectData.TriggerStage.None,
                "on_combat_turn_inert" => StatusEffectData.TriggerStage.OnCombatTurnInert,
                "on_pre_movement" => StatusEffectData.TriggerStage.OnPreMovement,
                "on_pre_attacked" => StatusEffectData.TriggerStage.OnPreAttacked,
                "on_attacked" => StatusEffectData.TriggerStage.OnAttacked,
                "on_pre_attacking" => StatusEffectData.TriggerStage.OnPreAttacking,
                "on_post_combat_regen" => StatusEffectData.TriggerStage.OnPostCombatRegen,
                "on_post_combat_poison" => StatusEffectData.TriggerStage.OnPostCombatPoison,
                "on_ambush" => StatusEffectData.TriggerStage.OnAmbush,
                "on_relentless" => StatusEffectData.TriggerStage.OnRelentless,
                "on_multistrike" => StatusEffectData.TriggerStage.OnMultistrike,
                "on_attract_damage" => StatusEffectData.TriggerStage.OnAttractDamage,
                "on_combat_turn_dazed" => StatusEffectData.TriggerStage.OnCombatTurnDazed,
                "on_post_room_combat" => StatusEffectData.TriggerStage.OnPostRoomCombat,
                "on_attack_target_mode_requested" => StatusEffectData.TriggerStage.OnAttackTargetModeRequested,
                "on_monster_team_turn_begin" => StatusEffectData.TriggerStage.OnMonsterTeamTurnBegin,
                "on_death" => StatusEffectData.TriggerStage.OnDeath,
                "on_post_attacking" => StatusEffectData.TriggerStage.OnPostAttacking,
                "on_pre_character_trigger" => StatusEffectData.TriggerStage.OnPreCharacterTrigger,
                "on_pre_attacked_spell_shield" => StatusEffectData.TriggerStage.OnPreAttackedSpellShield,
                "on_pre_attacked_damage_shield" => StatusEffectData.TriggerStage.OnPreAttackedDamageShield,
                "on_combat_turn_spark" => StatusEffectData.TriggerStage.OnCombatTurnSpark,
                "on_pre_attacked_armor" => StatusEffectData.TriggerStage.OnPreAttackedArmor,
                "on_pre_attacked_fragile" => StatusEffectData.TriggerStage.OnPreAttackedFragile,
                "on_pre_eaten" => StatusEffectData.TriggerStage.OnPreEaten,
                "on_post_eaten" => StatusEffectData.TriggerStage.OnPostEaten,
                "on_almost_post_room_combat" => StatusEffectData.TriggerStage.OnAlmostPostRoomCombat,
                "on_healed" => StatusEffectData.TriggerStage.OnHealed,
                "on_pre_flat_damage_increase" => StatusEffectData.TriggerStage.OnPreFlatDamageIncrease,
                "on_hit" => StatusEffectData.TriggerStage.OnHit,
                "on_post_spawn" => StatusEffectData.TriggerStage.OnPostSpawn,
                "on_pre_attacked_life_link" => StatusEffectData.TriggerStage.OnPreAttackedLifeLink,
                "on_pre_attacked_titan_skin" => StatusEffectData.TriggerStage.OnPreAttackedTitanSkin,
                _ => null
            };
        }

        public static CardState.UpgradeDisabledReason? ParseUpgradeDisabledReason(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            val = val.ToLower();
            return val switch
            {
                "none" => CardState.UpgradeDisabledReason.NONE,
                "card_type" => CardState.UpgradeDisabledReason.CardType,
                "no_slots" => CardState.UpgradeDisabledReason.NoSlots,
                "not_eligible" => CardState.UpgradeDisabledReason.NotEligible,
                "animation_active" => CardState.UpgradeDisabledReason.AnimationActive,
                "does_not_apply_status_effects" => CardState.UpgradeDisabledReason.DoesNotApplyStatusEffects,
                _ => null
            };
        }

        public static RelicData.RelicLoreTooltipStyle? ParseRelicLoreTooltipStyle(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            val = val.ToLower();
            return val switch
            {
                "herzal" => RelicData.RelicLoreTooltipStyle.Herzal,
                "malicka" => RelicData.RelicLoreTooltipStyle.Malicka,
                "heph" => RelicData.RelicLoreTooltipStyle.Heph,
                _ => null
            };
        }
        public static SpecialCharacterType? ParseSpecialCharacterType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            val = val.ToLower();
            return val switch
            {
                "none" => SpecialCharacterType.None,
                "outer_train_boss" => SpecialCharacterType.HarderOuterTrainBossCharacter,
                "treasure_and_traitor" => SpecialCharacterType.TreasureAndTraitorCharacters,
                _ => null
            };
        }
        public static RarityTicketType? ParseRarityTicketType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }

            val = val.ToLower();
            var values = val.Split('|', StringSplitOptions.RemoveEmptyEntries)
                           .Select(v => v.Trim())
                           .ToList();

            RarityTicketType result = RarityTicketType.None;
            foreach (var value in values)
            {
                RarityTicketType? flag = value switch
                {
                    "none" => RarityTicketType.None,
                    "card" => RarityTicketType.Card,
                    "enhancer" => RarityTicketType.Enhancer,
                    "relic" => RarityTicketType.Relic,
                    _ => null
                };

                if (flag == null)
                {
                    return null;
                }

                result |= flag.Value;
            }

            return result;
        }

        public static RunState.ClassType? ParseClassType(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            val = val.ToLower();
            var values = val.Split('|', StringSplitOptions.RemoveEmptyEntries)
                           .Select(v => v.Trim())
                           .ToList();
            var result = RunState.ClassType.None;
            foreach (var value in values)
            {
                RunState.ClassType? classType = value switch
                {
                    "none" => RunState.ClassType.None,
                    "main" => RunState.ClassType.MainClass,
                    "subclass" => RunState.ClassType.SubClass,
                    "nonclass" => RunState.ClassType.NonClass,
                    _ => null
                };
                if (classType == null)
                {
                    return null;
                }
                result |= classType.Value;
            }
            return result;
        }

        public static CharacterChatterData.Gender ParseGender(this IConfigurationSection section, CharacterChatterData.Gender defaultValue)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                return defaultValue;
            }
            val = val.ToLower();
            CharacterChatterData.Gender? value = val switch
            {
                "male" => CharacterChatterData.Gender.Male,
                "female" => CharacterChatterData.Gender.Female,
                "neutral" => CharacterChatterData.Gender.Neutral,
                _ => null
            };
            if (value == null)
            {
                return defaultValue;
            }
            return value.Value;
        }

        private static Comparator? DoParseComparator(string? val)
        {
            if (string.IsNullOrEmpty(val))
                return null;
            return val.ToLower() switch
            {
                "less_than" => Comparator.LessThan,
                "greater_than" => Comparator.GreaterThan,
                "equal" => Comparator.Equal,
                _ => null
            };
        }

        public static Comparator? ParseComparator(this IConfigurationSection section)
        {
            var val = section.Value;
            if (string.IsNullOrEmpty(val))
            {
                if (!section.GetChildren().Any())
                {
                    return null;
                }
                Comparator? ret = 0;
                foreach (var child in section.GetChildren())
                {
                    var flag = DoParseComparator(child.Value);
                    if (flag == null)
                    {
                        return null;
                    }
                    ret |= flag.Value;
                }
                return ret;
            }
            else
            {
                var values = val.Split('|', StringSplitOptions.RemoveEmptyEntries)
                               .Select(v => v.Trim())
                               .ToList();

                Comparator result = 0;
                foreach (var value in values)
                {
                    Comparator? flag = DoParseComparator(value);
                    if (flag == null)
                    {
                        return null;
                    }
                    result |= flag.Value;
                }
                return result;
            }
        }
    }
}
