using BepInEx.Logging;
using Microsoft.Extensions.Configuration;

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

        public static string GetPath(this IConfiguration configuration)
        {
            return (configuration as IConfigurationSection)?.Path ?? "";
        }
    }
}
