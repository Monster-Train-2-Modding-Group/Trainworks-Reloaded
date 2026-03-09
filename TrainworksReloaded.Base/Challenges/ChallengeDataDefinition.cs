using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Challenges
{
    public class ChallengeDataDefinition(
        string key,
        SpChallengeData data,
        IConfiguration configuration,
        bool isOverride
    ) : IDefinition<SpChallengeData>
    {
        public string Key { get; set; } = key;
        public SpChallengeData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = !isOverride;
    }
}
