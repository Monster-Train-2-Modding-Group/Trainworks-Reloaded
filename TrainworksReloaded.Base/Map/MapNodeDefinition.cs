using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Map
{
    public class MapNodeDefinition(string key, MapNodeData data, IConfiguration configuration)
        : IDefinition<MapNodeData>
    {
        public string Key { get; set; } = key;
        public MapNodeData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
