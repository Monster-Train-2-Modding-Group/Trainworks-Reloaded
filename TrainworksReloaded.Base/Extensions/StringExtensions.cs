using BepInEx.Logging;

namespace TrainworksReloaded.Base.Extensions
{
    public static class StringExtensions
    {
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(StringExtensions));

        public static string GetId(this string key, string template, string id)
        {
            return key.GetId(template, "", id);
        }

        public static string GetId(this string key, string template, string subtemplate, string id)
        {
            if (id.StartsWith("@"))
            {
                Logger.LogWarning($"For mod_guid {key} type {template} we are attempting to create an id for {template} there should be no @ preceding this id {id}." +
                    " @ is only needed when referencing this id in other places not when defining it.");
            }
            return FormId(key, template, subtemplate, id);
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

        public static string ToId(this string baseString, string key, string template, string subtemplate)
        {
            if (baseString.StartsWith("@"))
            {
                return FormId(key, template, subtemplate, baseString.Substring(1));
            }
            else
            {
                return baseString;
            }
        }

        private static string FormId(string key, string template, string id)
        {
            return FormId(key, template, "", id);
        }


        private static string FormId(string key, string template, string subtemplate, string id)
        {
            switch (template)
            {
                case TemplateConstants.GameObject:
                    var fullId = subtemplate switch
                    {
                        TemplateConstants.SubtemplateGameObjectCharacterArt => $"Character_{key}_{id}",
                        TemplateConstants.SubtemplateGameObjectCardArt => $"CardArt_{key}_{id}",
                        TemplateConstants.SubtemplateGameObjectStoryArt => $"Event_Background_{key}_{id}",
                        TemplateConstants.SubtemplateGameObjectMapNodeIcon => $"POIart_Map_{key}_{id}",
                        TemplateConstants.SubtemplateGameObjectVfx => $"FX_{key}_{id}",
                        _ => null
                    };
                    if (fullId == null)
                    {
                        Logger.LogWarning($"Querying the id for a GameObject without its type {key} {id} this may cause lookups using this id to fail.");
                        return $"{key}-{template}-{id}";
                    }
                    return fullId;
                case TemplateConstants.StatusEffect:
                    return $"{key}_{id}".ToLowerInvariant();
                default:
                    return $"{key}-{template}-{id}";
            }
        }
    }
}
