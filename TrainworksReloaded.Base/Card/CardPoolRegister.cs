using HarmonyLib;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Card
{
    public class CardPoolRegister : Dictionary<string, CardPool>, IRegister<CardPool>
    {
        private readonly IModLogger<CardPoolRegister> logger;
        private readonly Dictionary<string, CardPool> VanillaCardPools = [];

        public CardPoolRegister(IModLogger<CardPoolRegister> logger)
        {
            this.logger = logger;
            VanillaCardPools.AddRange(Resources.FindObjectsOfTypeAll<CardPool>().ToDictionary(x => x.name, x => x));
            VanillaCardPools.Remove("ModdedPool");
            this.AddRange(VanillaCardPools);
        }

        public void Register(string key, CardPool item)
        {
            logger.Log(LogLevel.Info, $"Register Card Pool {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out CardPool? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            IsModded = !VanillaCardPools.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }
    }
}
