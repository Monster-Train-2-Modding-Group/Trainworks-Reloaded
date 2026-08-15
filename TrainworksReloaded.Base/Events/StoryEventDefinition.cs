using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Events
{
    public class StoryEventDefinition(string key, string id, StoryEventData data, IConfiguration configuration) : IDefinition<StoryEventData>
    {
        public string Id { get; set; } = id;
        public string Key { get; set; } = key;
        public StoryEventData Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public bool IsModded => true;
    }
}
