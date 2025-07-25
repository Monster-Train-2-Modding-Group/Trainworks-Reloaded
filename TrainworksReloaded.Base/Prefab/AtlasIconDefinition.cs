using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class AtlasIconDefinition(string key, Texture2D data, IConfiguration configuration)
        : IDefinition<Texture2D>
    {
        public string Key { get; set; } = key;
        public Texture2D Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
