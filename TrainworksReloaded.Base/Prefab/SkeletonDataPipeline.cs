using Microsoft.Extensions.Configuration;
using Spine.Unity;
using Spine;
using System.Collections.Generic;
using System.IO;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using System.Linq;
using TrainworksReloaded.Core.Extensions;
using System.Reflection;
using HarmonyLib;
using System.Data.SqlTypes;
using System;

namespace TrainworksReloaded.Base.Prefab
{
    public class SkeletonDataPipeline : IDataPipeline<IRegister<SkeletonDataAsset>, SkeletonDataAsset>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<SkeletonDataPipeline> logger;
        private static Lazy<Material> skeletonDefaultMaterial = new(() =>
        {
            return Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "UI_Daedalus_Idle_Material");
        });

        public SkeletonDataPipeline(PluginAtlas atlas, IModLogger<SkeletonDataPipeline> logger)
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<SkeletonDataAsset>> Run(IRegister<SkeletonDataAsset> service)
        {
            var definitions = new List<IDefinition<SkeletonDataAsset>>();
            foreach (var pluginDefinition in atlas.PluginDefinitions)
            {
                var key = pluginDefinition.Key;
                foreach (var configuration in pluginDefinition.Value.Configuration.GetSection("skeletons").GetChildren())
                {
                    var data = LoadSkeletonAsset(service, key, configuration);
                    if (data != null)
                        definitions.Add(data);
                }
            }
            return definitions;
        }

        private IDefinition<SkeletonDataAsset>? LoadSkeletonAsset(IRegister<SkeletonDataAsset> service, string key, IConfigurationSection configuration)
        {
            var id = configuration.GetSection("id").Value;
            var atlas_path = FindPath(key, configuration.GetSection("atlas_path").Value);
            var data_path = FindPath(key, configuration.GetSection("data_path").Value);
            
            if (atlas_path == null || id == null || data_path == null)
            {
                logger.Log(LogLevel.Error, $"Unable to load skeleton {id} atlas: {atlas_path} data: {data_path}");
                return null;
            }
            var shader = configuration.GetSection("shader").Value ?? "Shader Graphs/CharacterShader2.0 Graph";
            var name = key.GetId(TemplateConstants.SkeletonData, id);

            var atlasData = new TextAsset(File.ReadAllText(atlas_path));

            List<Texture2D> textures = [];

            var texture_paths = configuration.GetSection("texture_paths").GetChildren()
                .Select(x => x.ParseString())
                .Where(x => x != null);
            foreach (var path in texture_paths)
            {
                var fullpath = FindPath(key, path);
                var textureData = File.ReadAllBytes(fullpath);
                var texture2d = new Texture2D(2, 2, TextureFormat.RGBA32, -1, false);
                if (!texture2d.LoadImage(textureData))
                {
                    logger.Log(LogLevel.Error, $"Could not load file as texture {fullpath}");
                    return null;
                }
                texture2d.name = Path.GetFileNameWithoutExtension(fullpath);
                textures.Add(texture2d);
            }

            TextAsset skeletonData;
            if (data_path.EndsWith("json"))
            {
                var text = File.ReadAllText(data_path);
                skeletonData = new TextAsset(text)
                {
                    name = Path.GetFileName(data_path)
                };
            }
            else if (data_path.EndsWith("skel"))
            {
                var bytes = File.ReadAllBytes(data_path);
                // Hack to be able to load binary skeleton files, since TextAsset doesn't support binary data at runtime only through the Unity Editor.
                skeletonData = new TextAsset("SPINE64|" + Convert.ToBase64String(bytes))
                {
                    name = Path.GetFileName(data_path + ".base64")
                };
            }
            else
            {
                logger.Log(LogLevel.Error, $"File {data_path} not readable. The extension must be .json, .skel");
                return null;
            }
                
            var material = new Material(Shader.Find(shader))
            {
                name = $"{id}_Material"
            };
            material.CopyPropertiesFromMaterial(skeletonDefaultMaterial.Value);
            var spineAtlasAsset = SpineAtlasAsset.CreateRuntimeInstance(atlasData, textures.ToArray(), material, true);
            var skeletonDataAsset = SkeletonDataAsset.CreateRuntimeInstance(skeletonData, spineAtlasAsset, true);
            skeletonDataAsset.name = id;

            service.Register(name, skeletonDataAsset);

            return new SkeletonDataDefinition(key, skeletonDataAsset, configuration)
            {
                Id = id,
            };
        }

        private string? FindPath(string key, string? path)
        {
            if (path.IsNullOrEmpty())
            {
                return null; 
            }
            foreach (var directory in atlas.PluginDefinitions[key].AssetDirectories)
            {
                var fullpath = Path.Combine(directory, path);
                if (!File.Exists(fullpath))
                {
                    continue;
                }
                return fullpath;
            }
            return null;
        }
    }
}
