using Microsoft.Extensions.Configuration;

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
                var key = mod_reference ?? defaultKey;
                return id.ToId(key, template);
            }
        }

        public static ReferencedObject? ParseReference(this IConfigurationSection section)
        {
            string? id = section.Value ?? section.GetSection("id").Value;
            string? mod_reference = section.GetSection("mod_reference").Value;

            if (id.IsNullOrEmpty() || id == "null")
                return null;
            return new ReferencedObject(id!, mod_reference, section);
        }
    }
}
