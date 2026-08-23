using Microsoft.Extensions.Configuration;
using System.Reflection;
using UnityEngine;

namespace TrainworksReloaded.Core.Impl
{
    public class PluginDefinition
    {
        public IConfiguration Configuration { get; set; }
        public List<string> AssetDirectories { get; } = [];
        public Assembly? Assembly { get; set; }

        public PluginDefinition(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string? GetAssetDirectory()
        {
            return AssetDirectories.Count == 0 ? null : AssetDirectories[0];
        }
    }
}
