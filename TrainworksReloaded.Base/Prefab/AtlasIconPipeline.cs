using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class AtlasIconPipeline : IDataPipeline<IRegister<Texture2D>, Texture2D>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<SpritePipeline> logger;
        private readonly IRegister<AssetBundle> assetBundleRegister;

        public AtlasIconPipeline(PluginAtlas atlas, IModLogger<SpritePipeline> logger, IRegister<AssetBundle> assetBundleRegister)
        {
            this.atlas = atlas;
            this.logger = logger;
            this.assetBundleRegister = assetBundleRegister;
        }

        public List<IDefinition<Texture2D>> Run(IRegister<Texture2D> service)
        {
            var definitions = new List<IDefinition<Texture2D>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                var key = config.Key;
                var configuration = config.Value.Configuration;
                var assetPath = config.Value.GetAssetDirectory();
                foreach (var assetConfig in configuration.GetSection("atlas_icons").GetChildren())
                {
                    var definition = LoadIcon(service, assetPath, key, assetConfig);
                    if (definition != null)
                        definitions.Add(definition);
                }
            }
            return definitions;
        }

        public IDefinition<Texture2D>? LoadIcon(IRegister<Texture2D> service, string? assetDirectory, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").Value;
            var path = configuration.GetSection("path").ParseAssetFilePath(assetDirectory);
            if (path == null || id == null)
            {
                return null;
            }

            // These need to share the same naming scheme as the sprites in the sprites section.
            // StatusEffectManager uses the StatusEffectData/CharacterTrigger's Icon's sprite name to
            // query the TMP_SpriteAsset for an icon with the exact same name.
            // A sprite with ID will be used for StatusEffectData.Icon then an Atlas Icon needs to be 
            // registered with the same ID for use in Tooltips.
            var name = key.GetId(TemplateConstants.Sprite, id);

            Texture2D texture2d;
            if (path.IsFilePath())
            {
                var data = path.ReadBytes();
                if (data == null)
                {
                    logger.Log(LogLevel.Warning, $"Could not find/read asset at path: {path}. Atlas Icon will not exist.");
                    return null;
                }

                texture2d = new Texture2D(2, 2);
                if (!texture2d.LoadImage(data))
                {
                    logger.Log(LogLevel.Warning, $"Could not load image at path: {path}. Atlas Icon will not exist.");
                    return null;
                }
            }
            else if (assetBundleRegister.TryLookupName(path.bundleReference!.ToId(key, TemplateConstants.AssetBundle), out var bundle, out var _))
            {
                texture2d = bundle.LoadAsset<Texture2D>(path.path);
                if (texture2d == null)
                {
                    logger.Log(LogLevel.Warning, $"Could not load image at path: {path}. Atlas Icon will not exist.");
                    return null;
                }
            }
            else
            {
                return null;
            }
            texture2d.name = name;

            service.Register(name, texture2d);
            return new AtlasIconDefinition(key, texture2d, configuration)
            {
                Id = id,
            };
        }
    }
}
