using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using TrainworksReloaded.Core.Extensions;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Extensions
{
    public static class ParseAssetFilePathExtensions
    {
        public class AssetFilePath
        {
            public string path;
            public ReferencedObject? bundleReference;
            public IConfigurationSection context;

            public AssetFilePath(string path, ReferencedObject? bundle, IConfigurationSection context)
            {
                this.path = path;
                this.bundleReference = bundle;
                this.context = context;
            }

            public string? GetFilename()
            {
                if (bundleReference == null) return path;
                return null;
            }

            public byte[]? ReadBytes()
            {
                var filename = GetFilename();
                if (filename != null && File.Exists(filename))
                {
                    return File.ReadAllBytes(filename);
                }
                return null;
            }

            public string? ReadText()
            {
                var filename = GetFilename();
                if (filename != null && File.Exists(filename))
                {
                    return File.ReadAllText(filename);
                }
                return null;
            }

            public bool IsFilePath()
            {
                return bundleReference == null;
            }

            public override string ToString()
            {
                return bundleReference == null ? path : $"bundle: ({bundleReference.id} mod: {bundleReference.mod_reference})";
            }
        }

        public static AssetFilePath? ParseAssetFilePath(this IConfigurationSection section, string? base_dir)
        {
            if (base_dir == null) return null;
            string? id = section.Value;
            if (id != null)
            {   
                return new AssetFilePath(Path.Combine(base_dir, id), null, section);
            }
            var path = section.GetSection("asset_path").ParseString();
            var bundle = section.GetSection("bundle").ParseReference();
            if (bundle == null || path == null)
                return null;
            return new AssetFilePath(path, bundle, section);
        }
    }
}
