using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicDataDefinition(string key, RelicData data, IConfiguration configuration)
        : IDefinition<RelicData>
    {
        public string Key { get; set; } = key;
        public RelicData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}