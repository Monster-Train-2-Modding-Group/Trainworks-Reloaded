using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicDataRegister : Dictionary<string, RelicData>, IRegister<RelicData>
    {
        private readonly IModLogger<RelicDataRegister> logger;
        private readonly Lazy<SaveManager> SaveManager;

        public RelicDataRegister(GameDataClient client, IModLogger<RelicDataRegister> logger)
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

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => [],
            };
        }

        public void Register(string key, RelicData item)
        {
            logger.Log(LogLevel.Info, $"Register Relic {key}... ");
            var gamedata = SaveManager.Value.GetAllGameData();
            if (item is CollectableRelicData collectableRelic)
            {
                var RelicDatas =
                    (List<CollectableRelicData>)
                        AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(gamedata);
                RelicDatas.Add(collectableRelic);
            }
            else if (item is EnhancerData enhancerData)
            {
                var enhancerDatas =
                    (List<EnhancerData>)
                        AccessTools.Field(typeof(AllGameData), "enhancerDatas").GetValue(gamedata);
                enhancerDatas.Add(enhancerData);
            }
            else if (item is SinsData sinsData)
            {
                var sinsDatas =
                    (List<SinsData>)
                        AccessTools.Field(typeof(AllGameData), "sinsDatas").GetValue(gamedata);
                sinsDatas.Add(sinsData);
            }
            Add(key, item);
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out RelicData? lookup, [NotNullWhen(true)] out bool? IsModded)
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