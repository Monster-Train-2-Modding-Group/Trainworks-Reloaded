using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trials
{
    public class TrialDataDefinition(string key, TrialData data, IConfiguration configuration) : IDefinition<TrialData>
    {
        public string Key { get; set; } = key;
        public TrialData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
