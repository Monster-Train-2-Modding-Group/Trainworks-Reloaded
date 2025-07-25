using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trigger
{
    public class CharacterTriggerDefinition(
        string key,
        CharacterTriggerData data,
        IConfiguration configuration
    ) : IDefinition<CharacterTriggerData>
    {
        public string Key { get; set; } = key;
        public CharacterTriggerData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
