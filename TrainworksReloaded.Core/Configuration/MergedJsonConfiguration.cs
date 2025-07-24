using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
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
            }
            else if (node is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    LoadNode(array[i], data, $"{path}:{i}");
                }
            }
            else if (node is JValue value)
            {
                data[path] = value.ToString()!;
            }
        }
        private string CombinePath(string path, string key) => string.IsNullOrEmpty(path) ? key : $"{path}:{key}";
    }
}
