using BepInEx.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace TrainworksReloaded.Base.Extensions
{
    public static class ConfigurationExtensions
    {
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(ConfigurationExtensions));

        public static IConfigurationSection GetDeprecatedSection(this IConfiguration configuration, string name, string newName)
        {
            var section = configuration.GetSection(name);
            if (section.Exists())
            {
                Logger.LogWarning($"[Deprecation] Field name \"{name}\" is deprecated, use \"{newName}\" instead");
                return section;
            }
            else
            {
                return configuration.GetSection(newName);
            }
        }

        /// <summary>
        /// Gets the Configuration Path within the JSON configuration
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <returns>The path within the configuration.</returns>
        public static string GetPath(this IConfiguration configuration)
        {
            return (configuration as IConfigurationSection)?.Path ?? "";
        }

        public static IConfiguration GetExtension(this IConfiguration configuration, string name)
        {
            return configuration.GetSection("extensions").GetChildren().Where(xs => xs.GetSection(name).Exists()).Select(xs => xs.GetSection(name)).FirstOrDefault() as IConfiguration;
        }
    }
}
