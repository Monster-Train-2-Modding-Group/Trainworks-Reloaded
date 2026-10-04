using BepInEx.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrainworksReloaded.Core.Configuration
{
    public class MergedJsonConfigurationSource : IConfigurationSource
    {
        public IFileProvider? FileProvider { get; set; }
        public List<string> Paths { get; set; }
        public bool Optional { get; set; }

        public MergedJsonConfigurationSource()
        {
            Paths = [];
            Optional = true;
            FileProvider = null;
        }
        public MergedJsonConfigurationSource(IFileProvider fileProvider, bool optional = false, params string[] paths)
        {
            FileProvider = fileProvider;
            Optional = optional;
            Paths = [.. paths];
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new MergedJsonConfigurationProvider(this);
        }

        public void EnsureDefaults(IConfigurationBuilder builder)
        {
            FileProvider ??= builder.GetFileProvider();
        }
    }

    public class MergedJsonConfigurationProvider : ConfigurationProvider
    {
        private ManualLogSource logger = Logger.CreateLogSource("MergedJsonConfigurationProvider");

        public MergedJsonConfigurationSource Source { get; }

        public MergedJsonConfigurationProvider(MergedJsonConfigurationSource source)
        {
            Source = source;
        }

        public override void Load()
        {
            JObject? mergedJson = null;

            foreach (var path in Source.Paths)
            {
                try
                {
                    var info = Source.FileProvider?.GetFileInfo(path) ?? null;
                    if (info != null && info.Exists)
                    {
                        using var stream = info.CreateReadStream();
                        using var reader = new StreamReader(stream);
                        var currentJson = JObject.Parse(reader.ReadToEnd());

                        if (mergedJson == null)
                        {
                            mergedJson = currentJson;
                        }
                        else if (currentJson != null)
                        {
                            mergedJson.Merge(currentJson);
                        }
                    }
                    else
                    {
                        logger.LogError($"Could not find file {path} it does not exist. If this messsage is displayed in a released mod, please contact the author. The path to this file does not exist or the thunderstore package is incorrect.");
                    }
                }
                catch (JsonReaderException e)
                {
                    throw new Exception($"Failed to parse file {path} due to exception", e);
                }
            }

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            LoadNode(mergedJson, data, "");

            Data = data;
        }


        private void LoadNode(JToken? node, IDictionary<string, string?> data, string path)
        {
            if (node is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    LoadNode(property.Value, data, CombinePath(path, property.Name));
                }
                MarkIfEmpty(obj.Count == 0, data, path);
            }
            else if (node is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    LoadNode(array[i], data, $"{path}:{i}");
                }
                MarkIfEmpty(array.Count == 0, data, path);
            }
            else if (node is JValue value)
            {
                data[path] = value.ToString()!;
            }
        }

        /// <summary>
        /// An empty array or object produces no child keys, so without a marker the section
        /// would be indistinguishable from an omitted one: IConfigurationSection.Exists() would
        /// return false and "override": "replace" with an empty list would keep the vanilla list.
        /// Storing an empty string makes Exists() return true while GetChildren() stays empty.
        /// The root has no path, so it is never marked.
        /// </summary>
        private static void MarkIfEmpty(bool isEmpty, IDictionary<string, string?> data, string path)
        {
            if (isEmpty && !string.IsNullOrEmpty(path))
            {
                data[path] = "";
            }
        }
        private string CombinePath(string path, string key) => string.IsNullOrEmpty(path) ? key : $"{path}:{key}";
    }
}
