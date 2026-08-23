using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class SpritePipeline : IDataPipeline<IRegister<Sprite>, Sprite>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<SpritePipeline> logger;
        private readonly IRegister<AssetBundle> assetBundleRegister;
        private static readonly HashSet<string> OLDER_MODS = [
            "StewardClan.Plugin",
            "SweetkinBackOnTrack.Plugin"
        ];
        private static readonly Dictionary<string?, TextureWrapMode> StringToWrapMode = new()
        {
            ["clamp"] = TextureWrapMode.Clamp,
            ["repeat"] = TextureWrapMode.Repeat,
            ["mirror"] = TextureWrapMode.Mirror,
            ["mirror_once"] = TextureWrapMode.MirrorOnce,
        };
        private static readonly Dictionary<string?, SpriteMeshType> StringToMeshType = new()
        {
            ["full_rect"] = SpriteMeshType.FullRect,
            ["tight"] = SpriteMeshType.Tight,
        };

        public SpritePipeline(PluginAtlas atlas, IModLogger<SpritePipeline> logger, IRegister<AssetBundle> assetBundleRegister)
        {
            this.atlas = atlas;
            this.logger = logger;
            this.assetBundleRegister = assetBundleRegister;
        }

        public List<IDefinition<Sprite>> Run(IRegister<Sprite> service)
        {
            var definitions = new List<IDefinition<Sprite>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                var key = config.Key;
                var configuration = config.Value.Configuration;
                var assetPath = config.Value.GetAssetDirectory();
                foreach (var spriteConfig in configuration.GetSection("sprites").GetChildren())
                {
                    var definition = LoadSprite(service, assetPath, key, spriteConfig);
                    if (definition != null)
                        definitions.Add(definition);
                }
            }
            return definitions;
        }

        private SpriteDefinition? LoadSprite(IRegister<Sprite> service, string? assetDirectory, string key, IConfiguration spriteConfig)
        {
            var id = spriteConfig.GetSection("id").Value;
            var path = spriteConfig.GetSection("path").ParseAssetFilePath(assetDirectory);
            if (path == null || id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.Sprite, id);

            Sprite sprite;
            if (path.IsFilePath())
            {
                var data = path.ReadBytes();
                if (data == null)
                {
                    logger.Log(LogLevel.Warning, $"Could not find/read asset at path: {path}. Sprite will not exist.");
                    return null;
                }

                var texture2d = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture2d.LoadImage(data))
                {
                    logger.Log(LogLevel.Warning, $"Could not load image at path: {path}. Sprite will not exist.");
                    return null;
                }

                var pixelsPerUnit = spriteConfig.GetSection("pixels_per_unit").ParseFloat() ?? GetPixelsPerUnit(key);
                var pivot = spriteConfig.GetSection("pivot").ParseVec2(0.5f, 0.5f);
                uint extrude = (uint)(spriteConfig.GetSection("extrude").ParseInt() ?? 0);
                var spriteMeshType = StringToMeshType.GetValueOrDefault(spriteConfig.GetSection("mesh_type").Value?.ToLower() ?? "", SpriteMeshType.FullRect);
                var textureWrapMode = StringToWrapMode.GetValueOrDefault(spriteConfig.GetSection("wrap_mode").Value?.ToLower() ?? "", TextureWrapMode.Clamp);

                texture2d.name = name;
                texture2d.wrapMode = textureWrapMode;
                sprite = Sprite.Create(texture2d, new Rect(0, 0, texture2d.width, texture2d.height), pivot, pixelsPerUnit, extrude, spriteMeshType);
            }
            else if (assetBundleRegister.TryLookupName(path.bundleReference!.ToId(key, TemplateConstants.AssetBundle), out var bundle, out var _))
            {
                sprite = bundle.LoadAsset<Sprite>(path.path);
                if (sprite == null)
                {
                    logger.Log(LogLevel.Warning, $"Could not load image at path: {path}. Sprite will not exist.");
                    return null;
                }
            }
            else
            {
                return null;
            }
            sprite.name = name;

            service.Register(name, sprite);
            return new SpriteDefinition(key, sprite, spriteConfig)
            {
                Id = id,
                IsModded = true,
            };
        }

        private float GetPixelsPerUnit(string mod_guid)
        {
            if (OLDER_MODS.Contains(mod_guid))
                return 128;
            else
                return 100;
        }
    }
}
