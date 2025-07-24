using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterChatterRegister : Dictionary<string, CharacterChatterData>, IRegister<CharacterChatterData>
    {
        private readonly IModLogger<CharacterChatterRegister> logger;

        public CharacterChatterRegister(IModLogger<CharacterChatterRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, CharacterChatterData item)
        {
            logger.Log(LogLevel.Debug, $"Register Character Chatter {key}...");
            Add(key, item);
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CharacterChatterData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = true;
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
