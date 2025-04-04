using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Configuration;

namespace TrainworksReloaded.Core.Extensions{
    public static class ConfigurationExtensions
    {
        public static IConfigurationBuilder AddMergedJsonFile(
            this IConfigurationBuilder builder,
            params List<string> Paths)
        {
            return builder.AddMergedJsonFile(xs =>
            {
                xs.FileProvider = null;
                xs.Paths = Paths;
                xs.Optional = false;
            });
        }
        public static IConfigurationBuilder AddMergedJsonFile(this IConfigurationBuilder builder, Action<MergedJsonConfigurationSource>? configureSource)
            => builder.Add(configureSource);
    }
}