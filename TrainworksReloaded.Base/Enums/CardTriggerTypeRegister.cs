using MonoMod.Utils;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class CardTriggerTypeRegister
        : Dictionary<string, CardTriggerType>,
            IRegister<CardTriggerType>
    {
        private readonly IModLogger<CardTriggerTypeRegister> logger;
        private static readonly Dictionary<string, CardTriggerType> VanillaCardTriggerToEnum = new()
        {
            ["on_cast"] = CardTriggerType.OnCast,
            ["on_kill"] = CardTriggerType.OnKill,
            ["on_discard"] = CardTriggerType.OnDiscard,
            ["on_monster_death"] = CardTriggerType.OnMonsterDeath,
            ["on_any_monster_death_on_floor"] = CardTriggerType.OnAnyMonsterDeathOnFloor,
            ["on_any_hero_death_on_floor"] = CardTriggerType.OnAnyHeroDeathOnFloor,
            ["on_healed"] = CardTriggerType.OnHealed,
            ["on_player_damage_taken"] = CardTriggerType.OnPlayerDamageTaken,
            ["on_any_unit_death_on_floor"] = CardTriggerType.OnAnyUnitDeathOnFloor,
            ["on_treasure"] = CardTriggerType.OnTreasure,
            ["on_unplayed_negative"] = CardTriggerType.OnUnplayedNegative,
            ["on_feed"] = CardTriggerType.OnFeed,
            ["on_exhausted"] = CardTriggerType.OnExhausted,
            ["on_unplayed_positive"] = CardTriggerType.OnUnplayedPositive,
        };

        public CardTriggerTypeRegister(IModLogger<CardTriggerTypeRegister> logger)
        {
            this.logger = logger;
            this.AddRange(VanillaCardTriggerToEnum);
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

        public void Register(string key, CardTriggerType item)
        {
            logger.Log(LogLevel.Info, $"Register Card Trigger Enum ({key})");
            Add(key, item);
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CardTriggerType lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaCardTriggerToEnum.ContainsKey(identifier);
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
