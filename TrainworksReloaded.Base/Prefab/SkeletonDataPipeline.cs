using Microsoft.Extensions.Configuration;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseAssetFilePathExtensions;

namespace TrainworksReloaded.Base.Prefab
{
    public class SkeletonDataPipeline : IDataPipeline<IRegister<SkeletonDataAsset>, SkeletonDataAsset>
    {
        private readonly PluginAtlas atlas;
        private readonly IRegister<AssetBundle> assetBundleRegister;
        private readonly IModLogger<SkeletonDataPipeline> logger;
        private static Lazy<Material> skeletonDefaultMaterial = new(() =>
        {
            return Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "UI_Daedalus_Idle_Material");
        });

        public SkeletonDataPipeline(PluginAtlas atlas, IRegister<AssetBundle> assetBundleRegister, IModLogger<SkeletonDataPipeline> logger)
        {
            this.atlas = atlas;
            this.assetBundleRegister = assetBundleRegister;
            this.logger = logger;
        }

        public List<IDefinition<SkeletonDataAsset>> Run(IRegister<SkeletonDataAsset> service)
        {
            var definitions = new List<IDefinition<SkeletonDataAsset>>();
            foreach (var pluginDefinition in atlas.PluginDefinitions)
            {
                var key = pluginDefinition.Key;
                var assetDirectory = pluginDefinition.Value.GetAssetDirectory();
                foreach (var configuration in pluginDefinition.Value.Configuration.GetSection("skeletons").GetChildren())
                {
                    var data = LoadSkeletonAsset(service, assetDirectory, key, configuration);
                    if (data != null)
                        definitions.Add(data);
                }
            }
            return definitions;
        }

        private IDefinition<SkeletonDataAsset>? LoadSkeletonAsset(IRegister<SkeletonDataAsset> service, string? assetDirectory, string key, IConfigurationSection configuration)
        {
            var id = configuration.GetSection("id").Value;
            if (id == null)
                return null;

            var data_path = configuration.GetSection("data_path").ParseAssetFilePath(assetDirectory);
            if (data_path == null)
            {
                logger.Log(LogLevel.Error, $"Unable to load skeleton {id} data_path not found");
                return null;
            }

            var name = key.GetId(TemplateConstants.SkeletonData, id);

            var shader = configuration.GetSection("shader").Value ?? "Shader Graphs/CharacterShader2.0 Graph";

            SkeletonDataAsset? skeletonDataAsset;
            if (data_path.IsFilePath())
            {
                skeletonDataAsset = LoadSkeletonFromPath(configuration, assetDirectory, id, data_path, shader);
                if (skeletonDataAsset == null)
                {
                    return null;
                }
            }
            else if (assetBundleRegister.TryLookupName(data_path.bundleReference!.ToId(key, TemplateConstants.AssetBundle), out var bundle, out var _))
            {
                skeletonDataAsset = bundle.LoadAsset<SkeletonDataAsset>(data_path.path);
                if (skeletonDataAsset == null)
                {
                    logger.Log(LogLevel.Error, $"Unable to load skeleton {id} from AssetBundle {data_path}.");
                    return null;
                }
                ApplyCustomMaterial(skeletonDataAsset, shader, id);
            }
            else
            {
                logger.Log(LogLevel.Error, $"Unable to load skeleton {id}.");
                return null;
            }

            skeletonDataAsset.name = name;
            service.Register(name, skeletonDataAsset);
            return new SkeletonDataDefinition(key, skeletonDataAsset, configuration)
            {
                Id = id,
            };
        }

        public void ApplyCustomMaterial(SkeletonDataAsset skeletonDataAsset, string shader, string id)
        {
            foreach (AtlasAssetBase atlasAsset in skeletonDataAsset.atlasAssets)
            {
                if (atlasAsset is SpineAtlasAsset spineAtlas)
                {
                    for (int i = 0; i < spineAtlas.materials.Length; i++)
                    {
                        Material originalMat = spineAtlas.materials[i];

                        var newMat = new Material(Shader.Find(shader))
                        {
                            name = $"{id}_Material"
                        };
                        newMat.CopyPropertiesFromMaterial(skeletonDefaultMaterial.Value);

                        newMat.mainTexture = originalMat.mainTexture;

                        spineAtlas.materials[i] = newMat;
                    }

                    // Clear the cached runtime atlas so Spine regenerates pages with the new materials
                    spineAtlas.Clear();
                }
            }

            // Force the SkeletonDataAsset to clear runtime cached data
            skeletonDataAsset.Clear();
        }

        private SkeletonDataAsset? LoadSkeletonFromPath(IConfigurationSection configuration, string? assetDirectory, string id, AssetFilePath data_path, string shader)
        {
            var atlas_path = configuration.GetSection("atlas_path").ParseAssetFilePath(assetDirectory);
            var atlas_file_data = atlas_path?.ReadText();
            if (atlas_file_data == null)
            {
                logger.Log(LogLevel.Error, $"Unable to load skeleton {id} atlas: {atlas_path} file not found or failed to load.");
                return null;
            }
            var atlasData = new TextAsset(atlas_file_data);

            List<Texture2D> textures = [];
            var texture_paths = configuration.GetSection("texture_paths").GetChildren().Select(x => x.ParseAssetFilePath(assetDirectory)).Where(x => x != null);
            foreach (var path in texture_paths)
            {
                var textureData = path!.ReadBytes();
                if (textureData == null)
                {
                    logger.Log(LogLevel.Warning, $"Unable to load skeleton {id} texutre: {path} file not found or failed to load.");
                    continue;
                }
                var texture2d = new Texture2D(2, 2, TextureFormat.RGBA32, -1, false);
                if (!texture2d.LoadImage(textureData))
                {
                    logger.Log(LogLevel.Warning, $"Could not load file as texture {path}");
                    continue;
                }
                texture2d.name = Path.GetFileNameWithoutExtension(path.GetFilename());
                textures.Add(texture2d);
            }

            TextAsset skeletonData;
            var filename = data_path.GetFilename()!;
            if (filename.EndsWith("json"))
            {
                var text = data_path.ReadText()!;
                skeletonData = new TextAsset(text)
                {
                    name = Path.GetFileName(filename)
                };
            }
            else if (filename.EndsWith("skel"))
            {
                var bytes = data_path.ReadBytes()!;
                // Hack to be able to load binary skeleton files, since TextAsset doesn't support binary data at runtime only through the Unity Editor.
                skeletonData = new TextAsset("SPINE64|" + Convert.ToBase64String(bytes))
                {
                    name = Path.GetFileName(filename + ".base64")
                };
            }
            else
            {
                logger.Log(LogLevel.Error, $"File {data_path} not readable. The extension must be .json or .skel");
                return null;
            }

            var material = new Material(Shader.Find(shader))
            {
                name = $"{id}_Material"
            };
            material.CopyPropertiesFromMaterial(skeletonDefaultMaterial.Value);

            var spineAtlasAsset = SpineAtlasAsset.CreateRuntimeInstance(atlasData, textures.ToArray(), material, true);
            var skeletonDataAsset = SkeletonDataAsset.CreateRuntimeInstance(skeletonData, spineAtlasAsset, true);
            return skeletonDataAsset;
        }
    }
}
