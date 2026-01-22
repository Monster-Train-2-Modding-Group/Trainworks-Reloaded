using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeMaskRegister
        : Dictionary<string, CardUpgradeMaskData>,
            IRegister<CardUpgradeMaskData>
    {
        private readonly IModLogger<CardUpgradeMaskRegister> logger;
        private readonly Dictionary<string, CardUpgradeMaskData> VanillaFilters = [];

        public CardUpgradeMaskRegister(IModLogger<CardUpgradeMaskRegister> logger)
        {
            this.logger = logger;
            VanillaFilters.AddRange(Resources.FindObjectsOfTypeAll<CardUpgradeMaskData>().ToDictionary(x => x.name, x => x));
            this.AddRange(VanillaFilters);
        }

        public void Register(string key, CardUpgradeMaskData item)
        {
            logger.Log(LogLevel.Debug, $"Registering Upgrade Mask {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CardUpgradeMaskData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            IsModded = !VanillaFilters.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }
    }
}
