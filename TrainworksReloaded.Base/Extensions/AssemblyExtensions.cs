using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace TrainworksReloaded.Base.Extensions
{
    public static class AssemblyExtensions
    {
        public static Type? FindTypeByClassName(this Assembly assembly, string class_name)
        {
            return assembly.DefinedTypes.FirstOrDefault(t =>
                t.DeclaringType == null && t.Name == class_name
            );
        }

        public static readonly Assembly MT2Assembly = typeof(CardEffectDamage).Assembly;

        public static bool GetFullyQualifiedName<T>(
            this string className,
            Assembly? assembly,
            [NotNullWhen(true)] out string? fullyQualifiedName
        )
        {
            className = className.Replace("@", "");
            Type? foundType = null;
            bool baseGameType = false;
            fullyQualifiedName = null;
            if (assembly != null)
            {
                foundType = assembly.FindTypeByClassName(className);
            }
            if (foundType == null)
            {
                baseGameType = true;
                foundType = MT2Assembly.FindTypeByClassName(className);
            }
            if (foundType != null && typeof(T).IsAssignableFrom(foundType))
            {
                fullyQualifiedName = baseGameType ? className : foundType.AssemblyQualifiedName;
                return true;
            }
            return false;
        }
    }
}
