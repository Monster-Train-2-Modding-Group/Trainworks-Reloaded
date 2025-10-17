using MonoMod.Utils;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Class
{
    public class ClassCardStyleRegister
        : Dictionary<string, ClassCardStyle>,
            IRegister<ClassCardStyle>
    {
        private readonly IModLogger<ClassCardStyleRegister> logger;
        private static readonly Dictionary<string, ClassCardStyle> VanillaClassStyleToEnum = new()
        {
            ["none"] = ClassCardStyle.None,
            ["banished"] = ClassCardStyle.Banished,
            ["pyreborne"] = ClassCardStyle.Pyreborne,
            ["luna_coven"] = ClassCardStyle.LunaCoven,
            ["underlegion"] = ClassCardStyle.Underlegion,
            ["lazarus_league"] = ClassCardStyle.LazarusLeague,
            ["hellhorned"] = ClassCardStyle.Hellhorned,
            ["awoken"] = ClassCardStyle.Awoken,
            ["stygian_guard"] = ClassCardStyle.Stygian,
            ["umbra"] = ClassCardStyle.Umbra,
            ["melting_remnant"] = ClassCardStyle.Remnant
        };

        public ClassCardStyleRegister(IModLogger<ClassCardStyleRegister> logger)
        {
            this.logger = logger;
            this.AddRange(VanillaClassStyleToEnum);
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

        public void Register(string key, ClassCardStyle item)
        {
            logger.Log(LogLevel.Info, $"Register ClassCardStyle ({key})");
            Add(key, item);
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out ClassCardStyle lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = !VanillaClassStyleToEnum.ContainsKey(identifier);
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
