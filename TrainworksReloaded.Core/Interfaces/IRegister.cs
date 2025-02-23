﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TrainworksReloaded.Core.Interfaces
{
    /// <summary>
    /// A register is a Service for Looking up by either name or id certain game resources
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRegister<T>
    {
        public bool TryLookupName(string name, [NotNullWhen(true)] out T? lookup, [NotNullWhen(true)] out bool? isModded);
        public bool TryLookupId(string id, [NotNullWhen(true)] out T? lookup, [NotNullWhen(true)] out bool? isModded);
    }
}
