using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class GameObjectImportPipeline : IDataPipeline<IRegister<GameObject>, GameObject>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<AssetBundle> assetBundleRegister;
        private readonly IModLogger<GameObjectImportPipeline> logger;
        public GameObjectImportPipeline(PluginAtlas atlas, IRegister<AssetBundle> assetBundleRegister, IModLogger<GameObjectImportPipeline> logger)
        {
            this.atlas = atlas;
            this.assetBundleRegister = assetBundleRegister;
            this.logger = logger;
        }

        public List<IDefinition<GameObject>> Run(IRegister<GameObject> service)
        {
            var definitions = new List<IDefinition<GameObject>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                var key = config.Key;
                foreach (var gameObjectConfig in config.Value.Configuration.GetSection("game_objects").GetChildren())
                {
                    var id = gameObjectConfig.GetSection("id").Value;
                    if (id == null)
                    {
                        continue;
                    }
                    var name = key.GetId(TemplateConstants.GameObject, id);

                    GameObject? gameObject = null;

                    var presetPath = gameObjectConfig.GetSection("preset_path").ParseAssetFilePath(config.Value.GetAssetDirectory());
                    if (presetPath != null && !presetPath.IsFilePath())
                    {
                        if (assetBundleRegister.TryLookupName(presetPath.bundleReference!.ToId(key, TemplateConstants.AssetBundle), out var bundle, out var _))
                        {
                            gameObject = bundle.LoadAsset<GameObject>(presetPath.path);
                        }

                        if (gameObject == null)
                        {
                            logger.Log(LogLevel.Warning, $"Failed to load GameObject from AssetBundle {presetPath}. Creating a default GameObject.");
                            gameObject = new GameObject { name = name, layer = 0 };
                        }
                    }
                    else
                    {
                        gameObject = new GameObject { name = name, layer = 0 };
                    }

                    // Set to inactive to prevent it from being visible in the scene
                    GameObject.DontDestroyOnLoad(gameObject);

                    service.Register(name, gameObject);
                    var definition = new GameObjectDefinition(key, gameObject, gameObjectConfig)
                    {
                        Id = id,
                        IsModded = true,
                    };
                    definitions.Add(definition);
                }
            }
            return definitions;
        }
    }
}
