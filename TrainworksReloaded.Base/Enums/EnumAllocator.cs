using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;

namespace TrainworksReloaded.Base.Enums
{
    internal static class EnumAllocator<TEnum> where TEnum : Enum
    {
        private static IDictionary<string, TEnum> NameToEnum = new Dictionary<string, TEnum>();
        private static long NextEnumId = Enum.GetValues(typeof(TEnum)).Cast<object>().Select(Convert.ToInt64).Max() + 1;

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

        public static bool TryGetEnum(string key, string id, out TEnum val)
        {
            var name = key.GetId(typeof(TEnum).Name, id);
            return NameToEnum.TryGetValue(name, out val);
        }
    }


    // Public facing EnumAllocator for enums that aren't referenced by any GameData.
    // Want to keep this locked down only to those enums, everything else should have a Pipeline to create from JSON.
    // Otherwise the timing with creating enum from code and it being referenced from within JSON could be confusing to modders.
    public static class EnumAllocator
    {
        public static Damage.Type CreateDamageType(string key, string id)
            => EnumAllocator<Damage.Type>.CreateEnum(key, id);

        public static bool TryGetDamageType(string key, string id, out Damage.Type val)
            => EnumAllocator<Damage.Type>.TryGetEnum(key, id, out val);
    }
}
