using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Scenarios
{
    public class ScenarioRegister : Dictionary<string, ScenarioData>, IRegister<ScenarioData>
    {
        private readonly IModLogger<ScenarioRegister> logger;
        private readonly Lazy<List<ScenarioData>> Scenarios;

        public ScenarioRegister(GameDataClient client, IModLogger<ScenarioRegister> logger)
        {
            Scenarios = new Lazy<List<ScenarioData>>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    var scenarios = AccessTools.Field(typeof(AllGameData), "scenarioDatas").GetValue(provider.GetAllGameData()) as List<ScenarioData>;
                    return scenarios ?? [];
                }
                else
                {
                    return [];
                }
            });
            this.logger = logger;
        }

        public void Register(string key, ScenarioData item)
        {
            logger.Log(LogLevel.Info, $"Register Scenario ({key})");
            Scenarios.Value.Add(item);
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. Scenarios.Value.Select(scenario => scenario.name)],
                RegisterIdentifierType.GUID => [.. Scenarios.Value.Select(scenario => scenario.GetID())],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out ScenarioData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = false;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var scenario in Scenarios.Value)
                    {
                        if (scenario.name == identifier)
                        {
                            lookup = scenario;
                            IsModded = this.ContainsKey(scenario.name);
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    foreach (var scenario in Scenarios.Value)
                    {
                        if (scenario.GetID() == identifier)
                        {
                            lookup = scenario;
                            IsModded = this.ContainsKey(scenario.name);
                            return true;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}