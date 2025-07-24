using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicPoolDefinition(string key, RelicPool data, IConfiguration configuration)
        : IDefinition<RelicPool>
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = key;
        public RelicPool Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public bool IsModded => true;
    }
}
