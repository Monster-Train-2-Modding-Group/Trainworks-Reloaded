using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrainworksReloaded.Base.Prefab
{
    public class AssetBundlePipeline(PluginAtlas atlas) : IDataPipeline<IRegister<AssetBundle>, AssetBundle>
    {
        private readonly PluginAtlas atlas = atlas;

        public List<IDefinition<AssetBundle>> Run(IRegister<AssetBundle> service)
        {
            var definitions = new List<IDefinition<AssetBundle>>();
            foreach (var pluginDefinition in atlas.PluginDefinitions)
            {
                var key = pluginDefinition.Key;
                foreach (
                    var config in pluginDefinition
                        .Value.Configuration.GetSection("asset_bundles")
                        .GetChildren()
                )
                {
                    var id = config.GetSection("id").Value;
                    var path = config.GetSection("path").Value;
                    if (path == null || id == null)
                    {
                        continue;
                    }

                    var name = key.GetId(TemplateConstants.AssetBundle, id);

                    foreach (var directory in pluginDefinition.Value.AssetDirectories)
                    {
                        var fullpath = Path.Combine(directory, path);
                        if (!File.Exists(fullpath))
                        {
                            continue;
                        }
                        var data = AssetBundle.LoadFromFile(path);
                        service.Register(name, data);
                        var definition = new AssetBundleDefinition(key, data, config)
                        {
                            Id = id,
                        };
                        definitions.Add(definition);
                        break;
                    }
                }
            }
            return definitions;
        }
    }
}
