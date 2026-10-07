using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Core.Extensions;

namespace TrainworksReloaded.Base.Extensions
{
    public static class ParseReferenceExtensions
    {
        public class ReferencedObject
        {
            public string id;
            public string? mod_reference;
            public IConfigurationSection context;

            public ReferencedObject(string id, string? mod_reference, IConfigurationSection context)
            {
                this.id = id;
                this.mod_reference = mod_reference;
                this.context = context;
            }

            public string ToId(string defaultKey, string template)
            {
                return ToId(defaultKey, template, "");
            }

            public string ToId(string defaultKey, string template, string subtemplate)
            {
                var key = mod_reference ?? defaultKey;
                return id.ToId(key, template, subtemplate);
            }
        }

        /// <summary>
        /// Parses an id/mod_reference pair or an id
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static ReferencedObject? ParseReference(this IConfigurationSection section)
        {
            string? id = section.Value ?? section.GetSection("id").Value;
            string? mod_reference = section.GetSection("mod_reference").Value;

            if (id.IsNullOrEmpty() || id == "null")
                return null;
            return new ReferencedObject(id!, mod_reference, section);
        }

        /// <summary>
        /// Parses a Reference. Or a item/count pair with item being a reference.
        /// </summary>
        public static (ReferencedObject, int)? ParseReferenceWithCount(this IConfigurationSection section)
        {
            ReferencedObject? reference;
            if (section.GetSection("item").Exists() && section.GetSection("count").Exists())
            {
                int count = section.GetSection("count").ParseInt() ?? 0;
                count = Math.Max(1, count);
                reference = section.GetSection("item").ParseReference();
                return reference == null ? null : (reference, count);
            }
            reference = section.ParseReference();
            return reference == null ? null : (reference, 1);
        }

        public static IEnumerable<ReferencedObject?> ParseReferences(this IConfigurationSection section)
        {
            return section.GetChildren().Select(x => x.ParseReference()).Where(x => x != null).Cast<ReferencedObject>();
        }

        public static IEnumerable<(ReferencedObject, int)> ParseReferencesWithCounts(this IConfigurationSection section)
        {
            return section.GetChildren().Select(x => x.ParseReferenceWithCount()).Where(x => x != null).Cast<(ReferencedObject, int)>();
        }

        public static ReferencedObject? ParseAssetReference(this IConfigurationSection section)
        {
            var reference = section.ParseReference();
            if (reference != null)
                return reference;

            string? id = section.GetSection("asset_guid").Value;

            if (id.IsNullOrEmpty() || id == "null")
                return null;

            return new ReferencedObject(id!, null, section);
        }
    }
}
