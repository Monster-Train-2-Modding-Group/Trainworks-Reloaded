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

            var material = new Material(Shader.Find(shader));
            material.SetFloat("_BUILTIN_QueueControl", 0);
            material.SetFloat("_BUILTIN_QueueControl", 0f);
            material.SetFloat("_BUILTIN_QueueOffset", 0f);
            material.SetFloat("_Blackout_Slider", 0f);
            material.SetFloat("_Chosen", 0f);
            material.SetFloat("_ClipBottomAlpha", 1f);
            material.SetFloat("_ClipBottomV", 0f);
            material.SetFloat("_Cutoff", 0.1f);
            material.SetFloat("_Darken_Color", 1f);
            material.SetFloat("_DissolveAmount", 0f);
            material.SetFloat("_Dissolve_Border_Thickness", 1.14f);
            material.SetFloat("_Dissolve_Scale", 50f);
            material.SetFloat("_GrayScale_Factor", 0f);
            material.SetFloat("_Invalid", 0f);
            material.SetFloat("_OutlineMipLevel", 0f);
            material.SetFloat("_OutlineOpaqueAlpha", 1f);
            material.SetFloat("_OutlineReferenceTexWidth", 1024f);
            material.SetFloat("_OutlineSmoothness", 1f);
            material.SetFloat("_OutlineWidth", 3f);
            material.SetFloat("_Pulse_Frequency", 2f);
            material.SetFloat("_Pulse_Texture_Toggle", 0f);
            material.SetFloat("_RimLight_Sharpness", 0.26f);
            material.SetFloat("_Rim_Light_Opacity", 0.501f);
            material.SetFloat("_Rim_Light_Thickness_Thickness", 4f);
            material.SetFloat("_Selected", 0f);
            material.SetFloat("_Selection_Outline", 0f);
            material.SetFloat("_Shadow_Gradient_Offset", -3f);
            material.SetFloat("_Shadow_Gradient_Sharpness", 2.5f);
            material.SetFloat("_StencilComp", 8f);
            material.SetFloat("_StencilRef", 1f);
            material.SetFloat("_StraightAlphaInput", 0f);
            material.SetFloat("_ThresholdEnd", 0.25f);
            material.SetFloat("_Use8Neighbourhood", 1f);

            material.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
            material.SetColor("_Dissolve_Border_Color", new Color(1f, 0.1701541f, 0f, 0f));
            material.SetColor("_OutlineColor", new Color(1f, 1f, 0f, 1f));
            material.SetColor("_Pulse_Remap", new Color(0f, 1f, 0f, 0f));
            material.SetColor("_Pulse_Texture_Color", new Color(1f, 1f, 1f, 0f));
            material.SetColor("_RimLight_Color_1", new Color(0f, 0.254902f, 0.6980392f, 0f));
            material.SetColor("_Rim_Light_Color_2", new Color(0.6901961f, 0.2862745f, 0f, 0f));

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
