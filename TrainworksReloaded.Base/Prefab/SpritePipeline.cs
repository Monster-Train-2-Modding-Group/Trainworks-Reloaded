using System;
using System.Collections.Generic;
using System.IO;
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

        public SpritePipeline(PluginAtlas atlas, IModLogger<SpritePipeline> logger)
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<Sprite>> Run(IRegister<Sprite> service)
        {
            var definitions = new List<IDefinition<Sprite>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                var key = config.Key;
                foreach (
                    var spriteConfig in config
                        .Value.Configuration.GetSection("sprites")
                        .GetChildren()
                )
                {
                    var id = spriteConfig.GetSection("id").Value;
                    var path = spriteConfig.GetSection("path").Value;
                    if (path == null || id == null)
                    {
                        continue;
                    }
                    
                    var name = key.GetId(TemplateConstants.Sprite, id);

                    var pixelsPerUnit = spriteConfig.GetSection("pixels_per_unit").ParseFloat() ?? GetPixelsPerUnit(key);
                    var pivot = spriteConfig.GetSection("pivot").ParseVec2(0.5f, 0.5f);
                    uint extrude = (uint)(spriteConfig.GetSection("extrude").ParseInt() ?? 0);
                    var spriteMeshType = StringToMeshType.GetValueOrDefault(spriteConfig.GetSection("mesh_type").Value?.ToLower() ?? "", SpriteMeshType.FullRect);
                    var textureWrapMode = StringToWrapMode.GetValueOrDefault(spriteConfig.GetSection("wrap_mode").Value?.ToLower() ?? "", TextureWrapMode.Clamp);

                    foreach (var directory in config.Value.AssetDirectories)
                    {
                        var fullpath = Path.Combine(directory, path);
                        if (!File.Exists(fullpath))
                        {
                            logger.Log(LogLevel.Warning, $"Could not find asset at path: {fullpath}. Sprite will not exist.");
                            continue;
                        }
                        var data = File.ReadAllBytes(fullpath);
                        var texture2d = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!texture2d.LoadImage(data))
                        {
                            logger.Log(LogLevel.Warning, $"Could not load image at path: {fullpath}. Sprite will not exist.");
                            continue;
                        }
                        texture2d.name = name;
                        texture2d.wrapMode = textureWrapMode;
                        var sprite = Sprite.Create(
                            texture2d,
                            new Rect(0, 0, texture2d.width, texture2d.height),
                            pivot,
                            pixelsPerUnit,
                            extrude,
                            spriteMeshType
                        );
                        sprite.name = name;
                        service.Register(name, sprite);
                        var definition = new SpriteDefinition(key, sprite, spriteConfig)
                        {
                            Id = id,
                            IsModded = true,
                        };
                        definitions.Add(definition);
                        break;
                    }
                }
            }
            return definitions;
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
