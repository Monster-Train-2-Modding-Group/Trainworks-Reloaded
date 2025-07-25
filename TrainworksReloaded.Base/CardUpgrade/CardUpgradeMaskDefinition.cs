using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeMaskDefinition(
        string key,
        CardUpgradeMaskData data,
        IConfiguration configuration
    ) : IDefinition<CardUpgradeMaskData>
    {
        public string Key { get; set; } = key;
        public CardUpgradeMaskData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
