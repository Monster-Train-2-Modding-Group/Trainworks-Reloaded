using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Localization
{
    public class LanguageSourcePipeline : IDataPipeline<IRegister<LanguageSource>, LanguageSource>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<LanguageSourcePipeline> logger;

        public LanguageSourcePipeline(PluginAtlas atlas, IModLogger<LanguageSourcePipeline> logger)
        {
            this.atlas = atlas;
            this.logger = logger;
        }

        public List<IDefinition<LanguageSource>> Run(IRegister<LanguageSource> service)
        {
            var processList = new List<IDefinition<LanguageSource>>();
            foreach (var definition in atlas.PluginDefinitions)
            {
                var pluginConfig = definition.Value.Configuration;
                foreach (var config in pluginConfig.GetSection("language_sources").GetChildren())
                {
                    var paths = config.GetSection("csv_paths").GetChildren().Select(x => x.ParseString()).ToList();
                    var language = config.GetSection("language").ParseString();
                    if (paths.IsNullOrEmpty() || paths.Count < 2 || language == null)
                    {
                        continue;
                    }

                    Dictionary<string, string>? translations1 = ParseCSV(paths[0]!, definition.Value.AssetDirectories);
                    Dictionary<string, string>? translations2 = ParseCSV(paths[1]!, definition.Value.AssetDirectories);

                    if (translations1 == null || translations2 == null)
                    {
                        logger.Log(LogLevel.Error, $"No translations found for language {language}. No csv files found or could not be read.");
                        continue;
                    }
                    service.Register(language, new LanguageSource(language, [translations1, translations2]));
                }
            }
            return processList;
        }

        private Dictionary<string, string>? ParseCSV(string path, List<string> directories)
        {
            string? fullpath = null;
            foreach (var directory in directories)
            {
                fullpath = Path.Combine(directory, path);
                if (!File.Exists(fullpath))
                {
                    continue;
                }
            }
            if (fullpath == null)
                return null;

            Dictionary<string, string> dict = [];
            using (var reader = new StreamReader(fullpath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Read header
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    var key = csv.GetField(0)!;
                    var value = csv.GetField(1)!;
                    dict[key] = value;
                }
            }
            return dict;
        }
    }
}
