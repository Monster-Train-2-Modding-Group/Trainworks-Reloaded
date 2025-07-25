using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicPoolRegister : Dictionary<string, RelicPool>, IRegister<RelicPool>
    {
        private readonly IModLogger<RelicPoolRegister> logger;

        public RelicPoolRegister(GameDataClient client, IModLogger<RelicPoolRegister> logger)
        {
            this.logger = logger;
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
            IsModded = true;
            return this.TryGetValue(identifier, out lookup);
        }

        public static RelicPool? GetVanillaRelicPool(AllGameData allGameData, string poolName)
        {
            if (poolName == "megapool" || poolName == "MegaRelicPool")
            {
                return allGameData.GetBalanceData().GetFtueBlessingPool();
            }
            return null;
        }
    }
}
