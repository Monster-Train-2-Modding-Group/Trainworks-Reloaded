using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class CardTriggerTypeDefinition(
        string key,
        CardTriggerType data,
        IConfiguration configuration
    ) : IDefinition<CardTriggerType>
    {
        public string Key { get; set; } = key;
        public CardTriggerType Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
