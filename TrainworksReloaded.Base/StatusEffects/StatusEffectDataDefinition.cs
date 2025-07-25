using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.StatusEffects
{
    public class StatusEffectDataDefinition(string key, StatusEffectData data, IConfiguration configuration)
        : IDefinition<StatusEffectData>
    {
        public string Key { get; set; } = key;
        public StatusEffectData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
