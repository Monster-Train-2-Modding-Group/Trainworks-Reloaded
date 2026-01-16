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
        private readonly Lazy<List<CollectableRelicData>> collectables;
        private readonly Lazy<List<SinsData>> sins;
        private readonly Lazy<List<EnhancerData>> enhancers;
        private readonly Lazy<List<MutatorData>> mutators;
        private readonly Lazy<List<EndlessMutatorData>> endless_mutators;

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
            collectables = new(() =>
            {
                return (List<CollectableRelicData>)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(SaveManager.Value.GetAllGameData());
            });
            sins = new(() =>
            {
                return (List<SinsData>)AccessTools.Field(typeof(AllGameData), "sinsDatas").GetValue(SaveManager.Value.GetAllGameData());
            });
            enhancers = new(() =>
            {
                return (List<EnhancerData>)AccessTools.Field(typeof(AllGameData), "enhancerDatas").GetValue(SaveManager.Value.GetAllGameData());
            });
            mutators = new(() =>
            {
                return (List<MutatorData>)AccessTools.Field(typeof(AllGameData), "mutatorDatas").GetValue(SaveManager.Value.GetAllGameData());
            });
            endless_mutators = new(() =>
            {
                return (List<EndlessMutatorData>)AccessTools.Field(typeof(AllGameData), "endlessMutatorDatas").GetValue(SaveManager.Value.GetAllGameData());
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
            
            if (item is CollectableRelicData collectableRelic)
            {
                collectables.Value.Add(collectableRelic);
            }
            else if (item is EnhancerData enhancerData)
            {
                enhancers.Value.Add(enhancerData);
            }
            else if (item is SinsData sinsData)
            {
                sins.Value.Add(sinsData);
            }
            else if (item is MutatorData mutatorData)
            {
                mutators.Value.Add(mutatorData);
            }
            else if (item is EndlessMutatorData endlessMutatorData)
            {
                endless_mutators.Value.Add(endlessMutatorData);
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