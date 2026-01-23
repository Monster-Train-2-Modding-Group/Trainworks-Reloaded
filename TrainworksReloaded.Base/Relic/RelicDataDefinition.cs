using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicDataDefinition(string key, RelicData data, RelicData copyData, OverrideMode overrideMode, IConfiguration configuration)
        : IDefinition<RelicData>
    {
        public string Key { get; set; } = key;
        public RelicData Data { get; set; } = data;
        public RelicData CopyData { get; set; } = copyData;
        public OverrideMode Override { get; set; } = overrideMode;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}