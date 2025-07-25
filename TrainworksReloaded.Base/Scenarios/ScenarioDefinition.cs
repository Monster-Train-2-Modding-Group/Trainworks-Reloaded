using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Scenarios
{
    public class ScenarioDefinition(string key, ScenarioData data, ScenarioData copyData, OverrideMode overrideMode, IConfiguration configuration) : IDefinition<ScenarioData>
    {
        public string Key { get; set; } = key;
        public ScenarioData Data { get; set; } = data;
        public ScenarioData CopyData { get; set; } = copyData;
        public OverrideMode OverrideMode { get; set; } = overrideMode;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
