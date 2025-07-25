using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Reward
{
    public class RewardDataDefinition(string key, RewardData data, IConfiguration configuration)
        : IDefinition<RewardData>
    {
        public string Key { get; set; } = key;
        public RewardData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
