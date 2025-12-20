using System.Collections.Generic;
using System.IO;
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

        public AtlasIconPipeline(PluginAtlas atlas, IModLogger<SpritePipeline> logger)
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<Texture2D>> Run(IRegister<Texture2D> service)
        {
            var definitions = new List<IDefinition<Texture2D>>();
            foreach (var pluginDefinition in atlas.PluginDefinitions)
            {
                var key = pluginDefinition.Key;
                foreach (
                    var config in pluginDefinition
                        .Value.Configuration.GetSection("atlas_icons")
                        .GetChildren()
                )
                {
                    var id = config.GetSection("id").Value;
                    var path = config.GetSection("path").Value;
                    if (path == null || id == null)
                    {
                        continue;
                    }
                    // These need to share the same naming scheme as the sprites in the sprites section.
                    // StatusEffectManager uses the StatusEffectData/CharacterTrigger's Icon's sprite name to
                    // query the TMP_SpriteAsset for an icon with the exact same name.
                    // A sprite with ID will be used for StatusEffectData.Icon then an Atlas Icon needs to be 
                    // registered with the same ID for use in Tooltips.
                    var name = key.GetId(TemplateConstants.Sprite, id);

                    foreach (var directory in pluginDefinition.Value.AssetDirectories)
                    {
                        var fullpath = Path.Combine(directory, path);
                        if (!File.Exists(fullpath))
                        {
                            logger.Log(LogLevel.Warning, $"Could not find asset at path: {fullpath}. Atlas icon will not exist.");
                            continue;
                        }
                        var data = File.ReadAllBytes(fullpath);
                        var texture2d = new Texture2D(2, 2);
                        if (!texture2d.LoadImage(data))
                        {
                            logger.Log(LogLevel.Warning, $"Could not load image at path: {fullpath}. Atlas icon will not exist.");
                            continue;
                        }
                        texture2d.name = name;
                        service.Register(name, texture2d);
                        var definition = new AtlasIconDefinition(key, texture2d, config)
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
