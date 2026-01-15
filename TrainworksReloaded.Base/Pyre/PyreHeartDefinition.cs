using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Pyre
{
    public class PyreHeartDefinition(string key, PyreHeartData data, IConfiguration configuration) : IDefinition<PyreHeartData>
    {
        public string Key { get; set; } = key;
        public PyreHeartData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
