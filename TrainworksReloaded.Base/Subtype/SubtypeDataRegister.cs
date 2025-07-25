using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Subtype
{
    public class SubtypeDataRegister : Dictionary<string, SubtypeData>, IRegister<SubtypeData>
    {
        private readonly IModLogger<SubtypeDataRegister> logger;
        private readonly Lazy<SaveManager> SaveManager;

        public SubtypeDataRegister(GameDataClient client, IModLogger<SubtypeDataRegister> logger)
        {
            SaveManager = new Lazy<SaveManager>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    return provider;
                }
                else
                {
                    return new SaveManager();
                }
            });
            this.logger = logger;
        }

        public void Register(string key, SubtypeData item)
        {
            logger.Log(LogLevel.Info, $"Register Subtype {key}...");
            Add(key, item);
            SubtypeManager.AllData.Add(item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            List<string> ret = [.. this.Keys];
            if (identifierType == RegisterIdentifierType.GUID)
                ret.AddRange([.. SubtypeManager.AllData.Select(subtype => subtype.Key)]);
            return ret;
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out SubtypeData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = default;
            IsModded = true;
            if (this.TryGetValue(identifier, out lookup))
            {
                return true;
            }
            lookup = SubtypeManager.GetSubtypeData(identifier);
            IsModded = false;
            return lookup != null;
        }
    }
}
