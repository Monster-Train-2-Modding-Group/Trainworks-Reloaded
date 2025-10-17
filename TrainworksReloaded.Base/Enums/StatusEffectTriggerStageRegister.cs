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
    public class StatusEffectTriggerStageRegister : Dictionary<string, StatusEffectData.TriggerStage>, IRegister<StatusEffectData.TriggerStage>
    {
        private readonly IModLogger<StatusEffectTriggerStageRegister> logger;
        private static readonly Dictionary<string, StatusEffectData.TriggerStage> VanillaTriggerStageToEnum = new()
        {
            ["none"] = StatusEffectData.TriggerStage.None,
            ["on_combat_turn_inert"] = StatusEffectData.TriggerStage.OnCombatTurnInert,
            ["on_pre_movement"] = StatusEffectData.TriggerStage.OnPreMovement,
            ["on_pre_attacked"] = StatusEffectData.TriggerStage.OnPreAttacked,
            ["on_attacked"] = StatusEffectData.TriggerStage.OnAttacked,
            ["on_pre_attacking"] = StatusEffectData.TriggerStage.OnPreAttacking,
            ["on_post_combat_regen"] = StatusEffectData.TriggerStage.OnPostCombatRegen,
            ["on_post_combat_poison"] = StatusEffectData.TriggerStage.OnPostCombatPoison,
            ["on_ambush"] = StatusEffectData.TriggerStage.OnAmbush,
            ["on_relentless"] = StatusEffectData.TriggerStage.OnRelentless,
            ["on_multistrike"] = StatusEffectData.TriggerStage.OnMultistrike,
            ["on_attract_damage"] = StatusEffectData.TriggerStage.OnAttractDamage,
            ["on_combat_turn_dazed"] = StatusEffectData.TriggerStage.OnCombatTurnDazed,
            ["on_post_room_combat"] = StatusEffectData.TriggerStage.OnPostRoomCombat,
            ["on_attack_target_mode_requested"] = StatusEffectData.TriggerStage.OnAttackTargetModeRequested,
            ["on_monster_team_turn_begin"] = StatusEffectData.TriggerStage.OnMonsterTeamTurnBegin,
            ["on_death"] = StatusEffectData.TriggerStage.OnDeath,
            ["on_post_attacking"] = StatusEffectData.TriggerStage.OnPostAttacking,
            ["on_pre_character_trigger"] = StatusEffectData.TriggerStage.OnPreCharacterTrigger,
            ["on_pre_attacked_spell_shield"] = StatusEffectData.TriggerStage.OnPreAttackedSpellShield,
            ["on_pre_attacked_damage_shield"] = StatusEffectData.TriggerStage.OnPreAttackedDamageShield,
            ["on_combat_turn_spark"] = StatusEffectData.TriggerStage.OnCombatTurnSpark,
            ["on_pre_attacked_armor"] = StatusEffectData.TriggerStage.OnPreAttackedArmor,
            ["on_pre_attacked_fragile"] = StatusEffectData.TriggerStage.OnPreAttackedFragile,
            ["on_pre_eaten"] = StatusEffectData.TriggerStage.OnPreEaten,
            ["on_post_eaten"] = StatusEffectData.TriggerStage.OnPostEaten,
            ["on_almost_post_room_combat"] = StatusEffectData.TriggerStage.OnAlmostPostRoomCombat,
            ["on_healed"] = StatusEffectData.TriggerStage.OnHealed,
            ["on_pre_flat_damage_increase"] = StatusEffectData.TriggerStage.OnPreFlatDamageIncrease,
            ["on_hit"] = StatusEffectData.TriggerStage.OnHit,
            ["on_post_spawn"] = StatusEffectData.TriggerStage.OnPostSpawn,
            ["on_pre_attacked_life_link"] = StatusEffectData.TriggerStage.OnPreAttackedLifeLink,
            ["on_pre_attacked_titan_skin"] = StatusEffectData.TriggerStage.OnPreAttackedTitanSkin,
        };

        public StatusEffectTriggerStageRegister(IModLogger<StatusEffectTriggerStageRegister> logger)
        {
            this.logger = logger;
            this.AddRange(VanillaTriggerStageToEnum);
        }

        List<string> IRegister<StatusEffectData.TriggerStage>.GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => []
            };
        }

        void IRegisterableDictionary<StatusEffectData.TriggerStage>.Register(string key, StatusEffectData.TriggerStage item)
        {
            logger.Log(LogLevel.Info, $"Register StatusEffectTriggerStage Enum ({key})");
            Add(key, item);
        }

        bool IRegister<StatusEffectData.TriggerStage>.TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out StatusEffectData.TriggerStage lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaTriggerStageToEnum.ContainsKey(identifier);
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
