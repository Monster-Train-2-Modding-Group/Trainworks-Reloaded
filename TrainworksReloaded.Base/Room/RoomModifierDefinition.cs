using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Room
{
    public class RoomModifierDefinition(
        string key,
        RoomModifierData data,
        IConfiguration configuration
    ) : IDefinition<RoomModifierData>
    {
        public string Key { get; set; } = key;
        public RoomModifierData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; } = true;
    }
}
