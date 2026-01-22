using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TrainworksReloaded.Base
{
    public class ProviderDetails
    {
        public bool IsInitialized { get; set; }
        public IProvider Provider { get; set; }

        public ProviderDetails(bool isInit, IProvider provider)
        {
            this.IsInitialized = isInit;
            this.Provider = provider;
        }
    }

    public class GameDataClient : Dictionary<Type, ProviderDetails>, IClient
    {
        public void NewProviderAvailable(IProvider newProvider)
        {
            this[newProvider.GetType()] = new ProviderDetails(false, newProvider);
        }

        public void NewProviderFullyInstalled(IProvider newProvider)
        {
            if (this.ContainsKey(newProvider.GetType()))
            {
                this[newProvider.GetType()].IsInitialized = true;
            }
        }

        public void ProviderRemoved(IProvider removeProvider)
        {
            this.Remove(removeProvider.GetType());
        }

        public bool TryGetProvider<T>([NotNullWhen(true)] out T? provider) where T : IProvider
        {
            return TryGetProvider<T>(out provider, out _);
        }

        public bool TryGetProvider<T>([NotNullWhen(true)] out T? provider, out bool fullyInitialized) where T : IProvider
        {
            if (TryGetValue(typeof(T), out var details))
            {
                fullyInitialized = details.IsInitialized;
                provider = (T)details.Provider;
                return true;
            }
            fullyInitialized = false;
            provider = default;
            return false;
        }
    }
}
