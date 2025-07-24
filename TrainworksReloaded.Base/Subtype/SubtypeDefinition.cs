using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Subtype
{
    public class SubtypeDefinition(
        string key,
        SubtypeData data,
        IConfiguration configuration
    ) : IDefinition<SubtypeData>
    {
        public string Key { get; set; } = key;
        public SubtypeData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
