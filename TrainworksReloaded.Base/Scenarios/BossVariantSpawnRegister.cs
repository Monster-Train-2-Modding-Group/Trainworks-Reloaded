using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Scenarios
{
    public class BossVariantSpawnRegister : Dictionary<string, BossVariantSpawnData>, IRegister<BossVariantSpawnData>
    {
        private readonly IModLogger<BossVariantSpawnRegister> logger;
        private readonly Lazy<IReadOnlyList<ScenarioData>> Scenarios;
        private readonly Lazy<System.Reflection.FieldInfo> BossVariantField;

        public BossVariantSpawnRegister(GameDataClient client, IModLogger<BossVariantSpawnRegister> logger)
        {
            Scenarios = new Lazy<IReadOnlyList<ScenarioData>>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    return provider.GetAllGameData().GetAllScenarioDatas();
                }
                else
                {
                    return [];
                }
            });
            BossVariantField = new Lazy<System.Reflection.FieldInfo>(() =>
                {
                    return AccessTools.Field(typeof(ScenarioData), "bossVariant");
                }
            );
            this.logger = logger;
        }

        public void Register(string key, BossVariantSpawnData item)
        {
            logger.Log(LogLevel.Debug, $"Register BossVariantSpawnData ({key})");
            Add(key, item);
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out BossVariantSpawnData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var scenario in Scenarios.Value)
                    {
                        var bossVariant = BossVariantField.Value.GetValue(scenario) as BossVariantSpawnData;
                        if (bossVariant?.name == identifier)
                        {
                            lookup = bossVariant;
                            IsModded = false;
                        }
                    }
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    foreach (var scenario in Scenarios.Value)
                    {
                        var bossVariant = BossVariantField.Value.GetValue(scenario) as BossVariantSpawnData;
                        if (bossVariant?.name == identifier)
                        {
                            lookup = bossVariant;
                            IsModded = false;
                        }
                    }
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }
    }
}