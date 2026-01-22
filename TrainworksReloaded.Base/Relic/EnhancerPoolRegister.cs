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
    public class EnhancerPoolRegister : Dictionary<string, EnhancerPool>, IRegister<EnhancerPool>
    {
        private readonly IModLogger<EnhancerPoolRegister> logger;
        private readonly Dictionary<string, EnhancerPool> VanillaEnhancerPools = [];

        public EnhancerPoolRegister(IModLogger<EnhancerPoolRegister> logger)
        {
            this.logger = logger;
            VanillaEnhancerPools.AddRange(Resources.FindObjectsOfTypeAll<EnhancerPool>().ToDictionary(x => x.name, x => x));
            this.AddRange(VanillaEnhancerPools);
        }

        public void Register(string key, EnhancerPool item)
        {
            logger.Log(LogLevel.Info, $"Register Enhancer Pool {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out EnhancerPool? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            IsModded = !VanillaEnhancerPools.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }
    }
}
