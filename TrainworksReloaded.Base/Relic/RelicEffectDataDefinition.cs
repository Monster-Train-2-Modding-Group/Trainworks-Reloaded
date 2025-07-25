using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectDataDefinition(string key, RelicEffectData data, IConfiguration configuration)
        : IDefinition<RelicEffectData>
    {
        public string Key { get; set; } = key;
        public RelicEffectData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}