using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeDefinition(
        string key,
        CardUpgradeData data,
        IConfiguration configuration,
        bool isOverride
    ) : IDefinition<CardUpgradeData>
    {
        public string Key { get; set; } = key;
        public CardUpgradeData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = !isOverride;
    }
}
