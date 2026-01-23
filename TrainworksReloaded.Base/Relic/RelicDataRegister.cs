using HarmonyLib;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicDataRegister : Dictionary<string, RelicData>, IRegister<RelicData>
    {
        private readonly IModLogger<RelicDataRegister> logger;
        private readonly List<CollectableRelicData> collectables = [];
        private readonly List<SinsData> sins = [];
        private readonly List<EnhancerData> enhancers = [];
        private readonly List<MutatorData> mutators = [];
        private readonly List<EndlessMutatorData> endless_mutators = [];
        private readonly Dictionary<string, RelicData> VanillaRelicData = [];

        public RelicDataRegister(GameDataClient client, IModLogger<RelicDataRegister> logger)
        {
            this.logger = logger;
            SaveManager saveManager; 
            if (!client.TryGetProvider<SaveManager>(out var provider))
            {
                logger.Log(LogLevel.Error, "[CATASTROPHIC] Unable to get SaveManager instance");
                return;
            }
            saveManager = provider;

            collectables = (List<CollectableRelicData>)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(saveManager.GetAllGameData());
            sins = (List<SinsData>)AccessTools.Field(typeof(AllGameData), "sinsDatas").GetValue(saveManager.GetAllGameData());
            enhancers = (List<EnhancerData>)AccessTools.Field(typeof(AllGameData), "enhancerDatas").GetValue(saveManager.GetAllGameData());
            mutators = (List<MutatorData>)AccessTools.Field(typeof(AllGameData), "mutatorDatas").GetValue(saveManager.GetAllGameData());
            endless_mutators = (List<EndlessMutatorData>)AccessTools.Field(typeof(AllGameData), "endlessMutatorDatas").GetValue(saveManager.GetAllGameData());

            VanillaRelicData.AddRange(collectables.ToDictionary(x => x.name, x => x));
            VanillaRelicData.AddRange(sins.ToDictionary(x => x.name, x => x));
            VanillaRelicData.AddRange(enhancers.ToDictionary(x => x.name, x => x));
            VanillaRelicData.AddRange(mutators.ToDictionary(x => x.name, x => x));
            VanillaRelicData.AddRange(endless_mutators.ToDictionary(x => x.name, x => x));

            this.AddRange(VanillaRelicData);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Select(x => x.Value.GetID())],
                _ => [],
            };
        }

        public void Register(string key, RelicData item)
        {
            logger.Log(LogLevel.Info, $"Register Relic {key}... ");
            
            if (item is CollectableRelicData collectableRelic)
            {
                collectables.Add(collectableRelic);
            }
            else if (item is EnhancerData enhancerData)
            {
                enhancers.Add(enhancerData);
            }
            else if (item is SinsData sinsData)
            {
                sins.Add(sinsData);
            }
            else if (item is MutatorData mutatorData)
            {
                mutators.Add(mutatorData);
            }
            else if (item is EndlessMutatorData endlessMutatorData)
            {
                endless_mutators.Add(endlessMutatorData);
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
                    IsModded = !VanillaRelicData.ContainsKey(identifier);
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    IsModded = !VanillaRelicData.ContainsKey(identifier);
                    lookup = this.FirstOrDefault(x => x.Value.GetID() == identifier).Value;
                    return lookup != null;
                default:
                    return false;
            }
        }
    }
}