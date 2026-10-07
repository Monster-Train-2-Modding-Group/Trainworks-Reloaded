using Malee;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainworksReloaded.Base.Extensions
{
    public static class ReorderableArrayExtensions
    {
        public static void AddRange<T>(this ReorderableArray<T> array, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                array.Add(item);
            }
        }
    }
}
