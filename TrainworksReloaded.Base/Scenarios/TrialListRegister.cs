using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TrainworksReloaded.Base.Room;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using static RotaryHeart.Lib.DataBaseExample;

namespace TrainworksReloaded.Base.Scenarios
{
    public class TrialListRegister
        : Dictionary<string, TrialDataList>,
            IRegister<TrialDataList>
    {
        private readonly IModLogger<TrialListRegister> logger;
        private readonly Lazy<List<ScenarioData>> Scenarios;

        public TrialListRegister(GameDataClient client, IModLogger<TrialListRegister> logger)
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

        public void Register(string key, TrialDataList item)
        {
            logger.Log(LogLevel.Debug, $"Register TrialDataList ({key})");
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out TrialDataList? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var scenario in Scenarios.Value)
                    {
                        if (scenario.GetTrialDataList()?.ListName == identifier)
                        {
                            IsModded = false;
                            lookup = scenario.GetTrialDataList();
                            return true;
                        }
                    }
                    foreach (var item in this.Values)
                    {
                        if (item.ListName == identifier)
                        {
                            lookup = item;
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    foreach (var scenario in Scenarios.Value)
                    {
                        if (scenario.GetTrialDataList()?.ListName == identifier)
                        {
                            IsModded = false;
                            lookup = scenario.GetTrialDataList();
                            return true;
                        }
                    }
                    foreach (var item in this.Values)
                    {
                        if (item.ListName == identifier)
                        {
                            lookup = item;
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
