using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicPoolRegister : Dictionary<string, RelicPool>, IRegister<RelicPool>
    {
        private readonly IModLogger<RelicPoolRegister> logger;
        private readonly Dictionary<string, RelicPool> VanillaRelicPools = [];

        public RelicPoolRegister(IModLogger<RelicPoolRegister> logger)
        {
            this.logger = logger;
            VanillaRelicPools.AddRange(Resources.FindObjectsOfTypeAll<RelicPool>().ToDictionary(x => x.name, x => x));
            this.AddRange(VanillaRelicPools);
        }

        public void Register(string key, RelicPool item)
        {
            logger.Log(LogLevel.Info, $"Register Relic Pool {key}... ");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out RelicPool? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            IsModded = !VanillaRelicPools.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }
    }
}
