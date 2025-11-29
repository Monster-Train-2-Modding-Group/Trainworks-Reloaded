using BepInEx.Logging;

namespace TrainworksReloaded.Base.Extensions
{
    public static class StringExtensions
    {
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(StringExtensions));

        public static string GetId(this string key, string template, string id)
        {
            if (id.StartsWith("@"))
            {
                Logger.LogWarning($"For mod_guid {key} type {template} we are attempting to create an id for {template} there should be no @ preceding this id {id}." +
                    " @ is only needed when referencing this id in other places not when defining it.");
            }
            return FormId(key, template, id);
        }

        public static string ToId(this string baseString, string key, string template)
        {
            if (baseString.StartsWith("@"))
            {
                return FormId(key, template, baseString.Substring(1));
            }
            else
            {
                return baseString;
            }
        }

        private static string FormId(string key, string template, string id)
        {
            switch (template)
            {
                case TemplateConstants.StatusEffect:
                    return $"{key}_{id}".ToLowerInvariant();
                default:
                    return $"{key}-{template}-{id}";
            }
        }
    }
}
