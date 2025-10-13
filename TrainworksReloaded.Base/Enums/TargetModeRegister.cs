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
    public class TargetModeRegister : Dictionary<string, TargetMode>, IRegister<TargetMode>
    {
        private static readonly Dictionary<string, TargetMode> VanillaTargetModeToEnum = new()
        {
            ["room"] = TargetMode.Room,
            ["random_in_room"] = TargetMode.RandomInRoom,
            ["front_in_room"] = TargetMode.FrontInRoom,
            ["room_heal_targets"] = TargetMode.RoomHealTargets,
            ["self"] = TargetMode.Self,
            ["last_attacked_character"] = TargetMode.LastAttackedCharacter,
            ["front_with_status"] = TargetMode.FrontWithStatus,
            ["tower"] = TargetMode.Tower,
            ["back_in_room"] = TargetMode.BackInRoom,
            ["drop_target_character"] = TargetMode.DropTargetCharacter,
            ["draw_pile"] = TargetMode.DrawPile,
            ["discard"] = TargetMode.Discard,
            ["exhaust"] = TargetMode.Exhaust,
            ["eaten"] = TargetMode.Eaten,
            ["weakest"] = TargetMode.Weakest,
            ["last_attacker_character"] = TargetMode.LastAttackerCharacter,
            ["hand"] = TargetMode.Hand,
            ["last_drawn_card"] = TargetMode.LastDrawnCard,
            ["last_feeder_character"] = TargetMode.LastFeederCharacter,
            ["pyre"] = TargetMode.Pyre,
            ["last_targeted_characters"] = TargetMode.LastTargetedCharacters,
            ["last_damaged_characters"] = TargetMode.LastDamagedCharacters,
            ["last_spawned_morsel"] = TargetMode.LastSpawnedMorsel,
            ["front_in_all_rooms"] = TargetMode.FrontInAllRooms,
            ["front_in_room_and_room_above"] = TargetMode.FrontInRoomAndRoomAbove,
            ["weakest_all_rooms"] = TargetMode.WeakestAllRooms,
            ["strongest_all_rooms"] = TargetMode.StrongestAllRooms,
            ["last_equipped_character"] = TargetMode.LastEquippedCharacter,
            ["last_sacrificed_character"] = TargetMode.LastSacrificedCharacter,
        };

        private readonly IModLogger<TargetModeRegister> logger;

        public TargetModeRegister(IModLogger<TargetModeRegister> logger)
        {
            this.logger = logger;
            this.AddRange(VanillaTargetModeToEnum);
        }

        List<string> IRegister<TargetMode>.GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => []
            };
        }

        void IRegisterableDictionary<TargetMode>.Register(string key, TargetMode item)
        {
            logger.Log(LogLevel.Info, $"Register TargetMode Enum ({key})");
            Add(key, item);
        }

        bool IRegister<TargetMode>.TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out TargetMode lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaTargetModeToEnum.ContainsKey(identifier);
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
