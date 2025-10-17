using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Class
{
    public class ClassCardStyleDefinition(
        string key,
        ClassCardStyle data,
        IConfiguration configuration
    ) : IDefinition<ClassCardStyle>
    {
        public string Key { get; set; } = key;
        public ClassCardStyle Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
