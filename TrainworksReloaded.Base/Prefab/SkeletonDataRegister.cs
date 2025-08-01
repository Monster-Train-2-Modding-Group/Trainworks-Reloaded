using Spine.Unity;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Prefab
{
    public class SkeletonDataRegister : Dictionary<string, SkeletonDataAsset>, IRegister<SkeletonDataAsset>
    {
        private readonly IModLogger<SkeletonDataRegister> logger;

        public SkeletonDataRegister(IModLogger<SkeletonDataRegister> logger)
        {
            this.logger = logger;
        }

        public void Register(string key, SkeletonDataAsset item)
        {
            logger.Log(LogLevel.Debug, $"Register SkeletonDataAsset {key}");
            this.Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out SkeletonDataAsset? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }
    }
}
