using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Events
{
    public class StoryEventPoolRegister : Dictionary<string, StoryEventPoolData>, IRegister<StoryEventPoolData>
    {
        private readonly Dictionary<string, StoryEventPoolData> VanillaPools = [];

        public StoryEventPoolRegister()
        {
            VanillaPools.AddRange(Resources.FindObjectsOfTypeAll<StoryEventPoolData>().ToDictionary(x => x.name, x => x));
            this.AddRange(VanillaPools);
        }

        public void Register(string key, StoryEventPoolData item)
        {
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out StoryEventPoolData? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            IsModded = !VanillaPools.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }
    }
}
