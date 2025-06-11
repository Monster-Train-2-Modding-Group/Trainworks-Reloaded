using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicEffectConditionDefinition(string key, RelicEffectCondition data, IConfiguration configuration)
        : IDefinition<RelicEffectCondition>
    {
        public string Key { get; set; } = key;
        public RelicEffectCondition Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
} 