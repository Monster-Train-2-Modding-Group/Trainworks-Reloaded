using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class SpriteDefinition(string key, Sprite data, IConfiguration configuration)
        : IDefinition<Sprite>
    {
        public string Key { get; set; } = key;
        public Sprite Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded { get; set; }
    }
}
