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
    public class TooltipDesignTypeRegister : Dictionary<string, TooltipDesigner.TooltipDesignType>, IRegister<TooltipDesigner.TooltipDesignType>
    {
        private readonly IModLogger<TooltipDesignTypeRegister> logger;
        private static readonly Dictionary<string, TooltipDesigner.TooltipDesignType> VanillaTooltipDesignTypeToEnum = new()
        {
            ["default"] = TooltipDesigner.TooltipDesignType.Default,
            ["lore_herzal"] = TooltipDesigner.TooltipDesignType.LoreHerzal,
            ["boss"] = TooltipDesigner.TooltipDesignType.Boss,
            ["default_wide"] = TooltipDesigner.TooltipDesignType.DefaultWide,
            ["positive"] = TooltipDesigner.TooltipDesignType.Positive,
            ["negative"] = TooltipDesigner.TooltipDesignType.Negative,
            ["persistent"] = TooltipDesigner.TooltipDesignType.Persistent,
            ["trigger"] = TooltipDesigner.TooltipDesignType.Trigger,
            ["keyword"] = TooltipDesigner.TooltipDesignType.Keyword,
            ["lore_malicka"] = TooltipDesigner.TooltipDesignType.LoreMalicka,
            ["lore_heph"] = TooltipDesigner.TooltipDesignType.LoreHeph,
            ["default_mega_wide"] = TooltipDesigner.TooltipDesignType.DefaultMegaWide,
            ["state_modifier"] = TooltipDesigner.TooltipDesignType.StateModifier,
            ["title"] = TooltipDesigner.TooltipDesignType.Title,
            ["equipment"] = TooltipDesigner.TooltipDesignType.Equipment,
            ["ability"] = TooltipDesigner.TooltipDesignType.Ability,
            ["tip"] = TooltipDesigner.TooltipDesignType.Tip,
            ["boss_title"] = TooltipDesigner.TooltipDesignType.BossTitle,
            ["relic_title"] = TooltipDesigner.TooltipDesignType.RelicTitle,
        };

        public TooltipDesignTypeRegister(IModLogger<TooltipDesignTypeRegister> logger)
        {
            this.logger = logger;
            this.AddRange(VanillaTooltipDesignTypeToEnum);
        }

        List<string> IRegister<TooltipDesigner.TooltipDesignType>.GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => []
            };
        }

        void IRegisterableDictionary<TooltipDesigner.TooltipDesignType>.Register(string key, TooltipDesigner.TooltipDesignType item)
        {
            logger.Log(LogLevel.Info, $"Register TooltipDesignType Enum ({key})");
            Add(key, item);
        }

        bool IRegister<TooltipDesigner.TooltipDesignType>.TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out TooltipDesigner.TooltipDesignType lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaTooltipDesignTypeToEnum.ContainsKey(identifier);
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
