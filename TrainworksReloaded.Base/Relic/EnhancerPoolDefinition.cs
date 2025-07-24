using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerPoolDefinition(string key, EnhancerPool data, IConfiguration configuration)
        : IDefinition<EnhancerPool>
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = key;
        public EnhancerPool Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public bool IsModded => true;
    }
}
