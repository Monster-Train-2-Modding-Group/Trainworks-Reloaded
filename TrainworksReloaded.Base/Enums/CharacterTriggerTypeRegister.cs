using MonoMod.Utils;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Base.Trigger;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class CharacterTriggerTypeRegister
        : Dictionary<string, CharacterTriggerData.Trigger>,
            IRegister<CharacterTriggerData.Trigger>
    {
        private readonly IModLogger<CharacterTriggerRegister> logger;

        private static readonly Dictionary<string, CharacterTriggerData.Trigger> VanillaCharacterTriggerToEnum = new()
        {
            ["on_death"] = CharacterTriggerData.Trigger.OnDeath,
            ["post_combat"] = CharacterTriggerData.Trigger.PostCombat,
            ["on_spawn"] = CharacterTriggerData.Trigger.OnSpawn,
            ["on_attacking"] = CharacterTriggerData.Trigger.OnAttacking,
            ["on_kill"] = CharacterTriggerData.Trigger.OnKill,
            ["on_any_hero_death_on_floor"] = CharacterTriggerData.Trigger.OnAnyHeroDeathOnFloor,
            ["on_any_monster_death_on_floor"] = CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor,
            ["on_heal"] = CharacterTriggerData.Trigger.OnHeal,
            ["on_team_turn_begin"] = CharacterTriggerData.Trigger.OnTeamTurnBegin,
            ["pre_combat"] = CharacterTriggerData.Trigger.PreCombat,
            ["post_ascension"] = CharacterTriggerData.Trigger.PostAscension,
            ["post_combat_healing"] = CharacterTriggerData.Trigger.PostCombatHealing,
            ["on_hit"] = CharacterTriggerData.Trigger.OnHit,
            ["after_spawn_enchant"] = CharacterTriggerData.Trigger.AfterSpawnEnchant,
            ["post_descension"] = CharacterTriggerData.Trigger.PostDescension,
            ["on_any_unit_death_on_floor"] = CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor,
            ["card_spell_played"] = CharacterTriggerData.Trigger.CardSpellPlayed,
            ["card_monster_played"] = CharacterTriggerData.Trigger.CardMonsterPlayed,
            ["end_turn_pre_hand_discard"] = CharacterTriggerData.Trigger.EndTurnPreHandDiscard,
            ["on_feed"] = CharacterTriggerData.Trigger.OnFeed,
            ["on_eaten"] = CharacterTriggerData.Trigger.OnEaten,
            ["on_turn_begin"] = CharacterTriggerData.Trigger.OnTurnBegin,
            ["on_burnout"] = CharacterTriggerData.Trigger.OnBurnout,
            ["on_spawn_not_from_card"] = CharacterTriggerData.Trigger.OnSpawnNotFromCard,
            ["on_unscaled_spawn"] = CharacterTriggerData.Trigger.OnUnscaledSpawn,
            ["post_attempted_ascension"] = CharacterTriggerData.Trigger.PostAttemptedAscension,
            ["post_attempted_descension"] = CharacterTriggerData.Trigger.PostAttemptedDescension,
            ["on_hatched"] = CharacterTriggerData.Trigger.OnHatched,
            ["card_corrupt_played"] = CharacterTriggerData.Trigger.CardCorruptPlayed,
            ["corruption_added"] = CharacterTriggerData.Trigger.CorruptionAdded,
            ["on_armor_added"] = CharacterTriggerData.Trigger.OnArmorAdded,
            ["on_food_spawn"] = CharacterTriggerData.Trigger.OnFoodSpawn,
            ["card_exhausted"] = CharacterTriggerData.Trigger.CardExhausted,
            ["on_remove_hatch"] = CharacterTriggerData.Trigger.OnRemoveHatch,
            ["on_own_ability_activated"] = CharacterTriggerData.Trigger.OnOwnAbilityActivated,
            ["on_sentry"] = CharacterTriggerData.Trigger.OnSentry,
            ["regal_count_added"] = CharacterTriggerData.Trigger.RegalCountAdded,
            ["on_unit_ability_available"] = CharacterTriggerData.Trigger.OnUnitAbilityAvailable,
            ["on_unit_ability_unavailable"] = CharacterTriggerData.Trigger.OnUnitAbilityUnavailable,
            ["on_shift"] = CharacterTriggerData.Trigger.OnShift,
            ["on_equipment_added"] = CharacterTriggerData.Trigger.OnEquipmentAdded,
            ["on_equipment_removed"] = CharacterTriggerData.Trigger.OnEquipmentRemoved,
            ["on_deathwish"] = CharacterTriggerData.Trigger.OnDeathwish,
            ["on_deathwish_lost"] = CharacterTriggerData.Trigger.OnDeathwishLost,
            ["on_valiant"] = CharacterTriggerData.Trigger.OnValiant,
            ["on_encounter_complete"] = CharacterTriggerData.Trigger.OnEncounterComplete,
            ["on_pyregel_added"] = CharacterTriggerData.Trigger.OnPyregelAdded,
            ["on_moon_phase_shift"] = CharacterTriggerData.Trigger.OnMoonPhaseShift,
            ["on_equipment_added_to_ally"] = CharacterTriggerData.Trigger.OnEquipmentAddedToAny,
            ["on_equipment_added_to_any"] = CharacterTriggerData.Trigger.OnEquipmentAddedToAny,
            ["on_timebomb"] = CharacterTriggerData.Trigger.OnTimebomb,
            ["on_reanimated"] = CharacterTriggerData.Trigger.OnReanimated,
            ["on_graft_equipment_added"] = CharacterTriggerData.Trigger.OnGraftEquipmentAdded,
            ["on_new_status_effect_added"] = CharacterTriggerData.Trigger.OnNewStatusEffectAdded,
            ["on_queued_status_effect_to_add"] = CharacterTriggerData.Trigger.OnQueuedStatusEffectToAdd,
            ["on_moon_lit"] = CharacterTriggerData.Trigger.OnMoonLit,
            ["on_moon_shade"] = CharacterTriggerData.Trigger.OnMoonShade,
            ["on_moonlit_lost"] = CharacterTriggerData.Trigger.OnMoonlitLost,
            ["on_moonshade_lost"] = CharacterTriggerData.Trigger.OnMoonshadeLost,
            ["on_troop_added"] = CharacterTriggerData.Trigger.OnTroopAdded,
            ["on_troop_removed"] = CharacterTriggerData.Trigger.OnTroopRemoved,
            ["on_train_room_loop"] = CharacterTriggerData.Trigger.OnTrainRoomLoop,
            ["on_status_effect_changed"] = CharacterTriggerData.Trigger.OnStatusEffectChanged,
            ["on_attacking_before_damage"] = CharacterTriggerData.Trigger.OnAttackingBeforeDamage,
            ["on_silence"] = CharacterTriggerData.Trigger.OnSilence,
            ["on_silence_lost"] = CharacterTriggerData.Trigger.OnSilenceLost,
            ["on_pre_own_ability_activated"] = CharacterTriggerData.Trigger.OnPreOwnAbilityActivated,
        };

        public CharacterTriggerTypeRegister(IModLogger<CharacterTriggerRegister> logger)
        {
            this.logger = logger;
            this.AddRange(VanillaCharacterTriggerToEnum);
        }

        public void Register(string key, CharacterTriggerData.Trigger item)
        {
            logger.Log(LogLevel.Info, $"Register Character Trigger Enum ({key})");
            Add(key, item);
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CharacterTriggerData.Trigger lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaCharacterTriggerToEnum.ContainsKey(identifier);
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
