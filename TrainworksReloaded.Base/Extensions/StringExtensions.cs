using BepInEx.Logging;
using static RimLight;

namespace TrainworksReloaded.Base.Extensions
{
    public static class StringExtensions
    {
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(StringExtensions));

        /// <summary>
        /// Gets the fully qualified id of an item belonging to a Mod's GUID.
        /// Do not hardcode IDs! Use this function over attempting to form the id yourself as the format is defined per template gamedata type.
        /// For GameObject types this is not the overload to call, use the overload with subtemplate.
        /// </summary>
        /// <param name="key">Mod GUID</param>
        /// <param name="template">GameData type. Do not hardcode use the constant defined in TemplateConstants.</param>
        /// <param name="id">ID of the item. This should not include a leading @.</param>
        /// <returns>The fully qualified ID.</returns>
        public static string GetId(this string key, string template, string id)
        {
            return key.GetId(template, "", id);
        }

        /// <summary>
        /// Gets the fully qualified id of an item belonging to a Mod's GUID.
        /// Do not hardcode IDs! Use this function over attempting to form the id yourself as the format is defined per template gamedata type.
        /// </summary>
        /// <param name="key">Mod GUID</param>
        /// <param name="template">GameData type. Do not hardcode use the constant defined in TemplateConstants.</param>
        /// <param name="subtemplate">Subtemplate type this should be specified if referring to a GameObject. Use the constants defined in TemplateConstants.</param>
        /// <param name="id">ID of the item. This should not include a leading @.</param>
        /// <returns>The fully qualified ID.</returns>
        public static string GetId(this string key, string template, string subtemplate, string id)
        {
            if (id.StartsWith("@"))
            {
                Logger.LogWarning($"For mod_guid {key} type {template} we are attempting to create an id for {template} there should be no @ preceding this id {id}." +
                    " @ is only needed when referencing this id in other places not when defining it.");
            }
            return FormId(key, template, subtemplate, id);
        }


        /// <summary>
        /// This function should not be used directly. Use GetId with the plugin's GUID instead.
        /// </summary>
        /// <see cref="GetId(string, string, string, string)"/>
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

        /// <summary>
        /// This function should not be used directly. Use GetId with the plugin's GUID instead.
        /// </summary>
        /// <see cref="GetId(string, string, string, string)"/>
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

        /// <summary>
        /// Given a fullyQualified ID string does it belong to the mod's guid.
        /// </summary>
        /// <param name="fullyQualifiedId">The fully qualified ID usually obtained from </param>
        /// <param name="modGUID">Mod's GUID</param>
        /// <returns>true if the Mod defined the GameData referenced by fullyQualifiedId</returns>
        public static bool BelongsToMod(this string fullyQualifiedId, string modGUID, string? template = null)
        {
            if (fullyQualifiedId == null)
                return false;

            return template switch
            {
                TemplateConstants.GameObject => fullyQualifiedId.Contains(modGUID),
                TemplateConstants.StatusEffect => fullyQualifiedId.Contains(modGUID.ToLowerInvariant()),
                _ => fullyQualifiedId.Contains(modGUID),
            };
        }

        /// <summary>
        /// Given a fully qualified ID extracts the Mod's GUID from it.
        /// Calling this function is preferred over attempting to guess the structure yourself as its defined per template
        /// </summary>
        /// <param name="fullyQualifiedId">Fully Qualified GameData name</param>
        /// <param name="template">TemplateConstant sepcifying the type of GameObject do not hardcode.</param>
        /// <param name="subtemplate">Required if TemplateConstant is GameObject, specify the type of GameObject again do not hardcode.</param>
        /// <returns>The Mod GUID. (In the case of a StatusEffect it will be all lowercase)</returns>
        public static string ExtractModGuid(this string fullyQualifiedId, string? template, string? subtemplate)
        {
            if (string.IsNullOrEmpty(fullyQualifiedId))
            {
                return string.Empty;
            }

            if (template == TemplateConstants.GameObject)
            {
                string? prefix = subtemplate switch
                {
                    TemplateConstants.SubtemplateGameObjectCharacterArt => "Character_",
                    TemplateConstants.SubtemplateGameObjectCardArt => "CardArt_",
                    TemplateConstants.SubtemplateGameObjectStoryArt => "Event_Background_",
                    TemplateConstants.SubtemplateGameObjectMapNodeIcon => "POIart_Map_",
                    TemplateConstants.SubtemplateGameObjectVfx => "FX_",
                    _ => null
                };

                if (prefix != null && fullyQualifiedId.StartsWith(prefix))
                {
                    // Remainder is "{key}_{id}"
                    string remainder = fullyQualifiedId.Substring(prefix.Length);
                    int underscoreIdx = remainder.IndexOf('_');
                    return underscoreIdx >= 0 ? remainder.Substring(0, underscoreIdx) : remainder;
                }

                // Fallback for null subtemplate / unknown GameObject matches the default: "{key}-{template}-{id}"
                return ExtractFirstHyphenSegment(fullyQualifiedId);
            }

            if (template == TemplateConstants.StatusEffect)
            {
                // Format is "{key}_{id}".ToLowerInvariant()
                int underscoreIdx = fullyQualifiedId.IndexOf('_');
                return underscoreIdx >= 0 ? fullyQualifiedId.Substring(0, underscoreIdx) : fullyQualifiedId;
            }

            // Default format: "{key}-{template}-{id}"
            return ExtractFirstHyphenSegment(fullyQualifiedId);
        }
        private static string ExtractFirstHyphenSegment(string input)
        {
            int hyphenIdx = input.IndexOf('-');
            return hyphenIdx >= 0 ? input.Substring(0, hyphenIdx) : input;
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
