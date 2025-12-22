using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;

namespace TrainworksReloaded.Base.Enums
{
    /// <summary>
    /// Class that extends an enum and adds values to it. 
    /// TODO make public, but restrict access to enums that are definable in JSON / or referenced directly by other GameObjects.
    /// </summary>
    internal static class EnumAllocator<TEnum> where TEnum : Enum
    {
        private static IDictionary<string, TEnum> NameToEnum;
        private static long NextEnumId;

        static EnumAllocator()
        {
            NameToEnum = new Dictionary<string, TEnum>();
            NextEnumId = Enum.GetValues(typeof(TEnum)).Cast<object>().Select(Convert.ToInt64).Max() + 1;
        }

        internal static TEnum GetNext()
        {
            var id = NextEnumId;
            NextEnumId++;
            return (TEnum)Enum.ToObject(typeof(TEnum), id);
        }

        public static TEnum CreateEnum(string key, string id)
        {
            var name = key.GetId(typeof(TEnum).Name, id);
            var e = GetNext();
            NameToEnum.Add(name, e);
            return e;
        }

        public static bool TryGetEnum(string key, string id, out TEnum? val)
        {
            var name = key.GetId(typeof(TEnum).Name, id);
            return NameToEnum.TryGetValue(name, out val);
        }
    }
}
