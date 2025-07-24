using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trigger
{
    public class CardTriggerEffectDefinition(
        string key,
        CardTriggerEffectData data,
        IConfiguration configuration
    ) : IDefinition<CardTriggerEffectData>
    {
        public string Key { get; set; } = key;
        public CardTriggerEffectData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
