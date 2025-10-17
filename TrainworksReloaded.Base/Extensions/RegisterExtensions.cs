using BepInEx.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Extensions
{
    public static class RegisterExtensions
    {
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(RegisterExtensions));
        /// <summary>
        /// Try to lookup an item by name
        /// A Name is a human readable identifier for an item
        /// </summary>
        /// <param name="name">The name of the item to lookup</param>
        /// <param name="lookup">The item if found</param>
        /// <param name="IsModded">Whether the item is modded</param>
        public static bool TryLookupName<T>(
            this IRegister<T> register,
            string name,
            [NotNullWhen(true)] out T? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            bool ret = register.TryLookupIdentifier(name, RegisterIdentifierType.ReadableID, out lookup, out IsModded);
            if (!ret)
            {
                Logger.LogWarning($"Could not find identifier of type {typeof(T).Name} with id (name) {name}.");
                Logger.LogDebug($"{Environment.StackTrace}");
            }
            return ret;
        }
        /// <summary>
        /// Try to lookup an item by id
        /// An Id is a unique identifier for an item that is not guaranteed to be human readable. 
        /// </summary>
        /// <param name="id">The id of the item to lookup</param>
        /// <param name="lookup">The item if found</param>
        /// <param name="IsModded">Whether the item is modded</param>
        public static bool TryLookupId<T>(
            this IRegister<T> register,
            string id,
            [NotNullWhen(true)] out T? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            bool ret = register.TryLookupIdentifier(id, RegisterIdentifierType.GUID, out lookup, out IsModded);
            if (!ret)
            {
                Logger.LogWarning($"Could not find identifier of type {typeof(T).Name} with id (guid) {id}.");
                Logger.LogDebug($"{Environment.StackTrace}");
            }
            return ret;
        }
    }
}
