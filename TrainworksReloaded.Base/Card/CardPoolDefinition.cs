using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Card
{
    public class CardPoolDefinition(string key, CardPool data, IConfiguration configuration)
        : IDefinition<CardPool>
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = key;
        public CardPool Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public bool IsModded => true;
    }
}
