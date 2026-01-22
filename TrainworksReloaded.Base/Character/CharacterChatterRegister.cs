using MonoMod.Utils;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterChatterRegister : Dictionary<string, CharacterChatterData>, IRegister<CharacterChatterData>
    {
        private readonly IModLogger<CharacterChatterRegister> logger;
        private Dictionary<string, CharacterChatterData> VanillaChatter = [];

        public CharacterChatterRegister(IModLogger<CharacterChatterRegister> logger)
        {
            this.logger = logger;
            VanillaChatter.AddRange(Resources.FindObjectsOfTypeAll<CharacterChatterData>().ToDictionary(x => x.name, x => x));
            this.AddRange(VanillaChatter);
        }

        public void Register(string key, CharacterChatterData item)
        {
            logger.Log(LogLevel.Debug, $"Register Character Chatter {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CharacterChatterData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaChatter.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }

    }
}
