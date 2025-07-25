using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterDataDefinition(
        string key,
        CharacterData data,
        CharacterData copyData,
        IConfiguration configuration,
        OverrideMode overrideMode,
        bool modded
    ) : IDefinition<CharacterData>
    {
        public string Key { get; set; } = key;
        public CharacterData Data { get; set; } = data;
        public CharacterData CopyData { get; set; } = copyData;
        public OverrideMode Override { get; set; } = overrideMode;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = modded;
    }
}
