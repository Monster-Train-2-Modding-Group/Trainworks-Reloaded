using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class AssetBundlePipeline : IDataPipeline<IRegister<AssetBundle>, AssetBundle>
    {
        private readonly PluginAtlas atlas;
        private IModLogger<AssetBundlePipeline> logger;

        public AssetBundlePipeline(PluginAtlas atlas, IModLogger<AssetBundlePipeline> logger)
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<AssetBundle>> Run(IRegister<AssetBundle> service)
        {
            var definitions = new List<IDefinition<AssetBundle>>();
            foreach (var pluginDefinition in atlas.PluginDefinitions)
            {
                var key = pluginDefinition.Key;
                var assetDirectory = pluginDefinition.Value.GetAssetDirectory();
                var configuration = pluginDefinition.Value.Configuration;
                foreach (var config in configuration.GetSection("asset_bundles").GetChildren())
                {
                    LoadAssetBundle(service, assetDirectory, key, config);
                }
            }
            return definitions;
        }

        private void LoadAssetBundle(IRegister<AssetBundle> service, string? assetDirectory, string key, IConfigurationSection configuration)
        {
            var id = configuration.GetSection("id").Value;
            var paths = configuration.GetSection("paths");
            if (!paths.Exists() || id == null)
            {
                return;
            }

            var name = key.GetId(TemplateConstants.AssetBundle, id);

            string? path;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    path = paths.GetSection("windows").Value;
                    break;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    path = paths.GetSection("macosx").Value;
                    break;
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    path = paths.GetSection("linux").Value;
                    break;
                default:
                    path = paths.GetSection("windows").Value;
                    break;
            }

            if (path == null)
            {
                logger.Log(LogLevel.Error, $"Unable to find asset bundle for {Application.platform}. An Assetbundle needs to be provided for this platform.");
                return;
            }


            var fullpath = Path.Combine(assetDirectory, path);
            if (!File.Exists(fullpath))
            {
                logger.Log(LogLevel.Warning, $"Could not find asset at path: {fullpath}. AssetBundle will not exist.");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(fullpath);
            if (bundle == null)
            {
                logger.Log(LogLevel.Warning, $"Could not load AssetBundle at path: {fullpath}.");
                return;
            }

            bundle.name = name;
            service.Register(name, bundle);
        }
    }
}
