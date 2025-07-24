using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class CharacterTriggerTypeDefinition(
        string key,
        CharacterTriggerData.Trigger data,
        IConfiguration configuration
    ) : IDefinition<CharacterTriggerData.Trigger>
    {
        public string Key { get; set; } = key;
        public CharacterTriggerData.Trigger Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
