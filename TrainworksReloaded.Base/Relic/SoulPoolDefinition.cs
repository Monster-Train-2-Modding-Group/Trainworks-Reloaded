using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class SoulPoolDefinition(string key, SoulPool data, IConfiguration configuration)
        : IDefinition<SoulPool>
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = key;
        public SoulPool Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public bool IsModded => true;
    }
}
