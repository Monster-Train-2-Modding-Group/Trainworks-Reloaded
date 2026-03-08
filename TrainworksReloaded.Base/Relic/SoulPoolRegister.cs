using MonoMod.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class SoulPoolRegister : Dictionary<string, SoulPool>, IRegister<SoulPool>
    {
        private readonly IModLogger<SoulPoolRegister> logger;
        private readonly Dictionary<string, SoulPool> VanillaSoulPools = [];

        public SoulPoolRegister(IModLogger<SoulPoolRegister> logger)
        {
            this.logger = logger;
            VanillaSoulPools.AddRange(Resources.FindObjectsOfTypeAll<SoulPool>().ToDictionary(x => x.name, x => x));
            this.AddRange(VanillaSoulPools);
        }

        public void Register(string key, SoulPool item)
        {
            logger.Log(LogLevel.Info, $"Register SoulPool {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out SoulPool? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            IsModded = !VanillaSoulPools.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }
    }
}
