using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Trials
{
    public class TrialDataRegister : Dictionary<string, TrialData>, IRegister<TrialData>
    {
        private readonly IModLogger<TrialDataRegister> logger;
        private readonly Lazy<List<TrialData>> Trials;

        public TrialDataRegister(GameDataClient client, IModLogger<TrialDataRegister> logger)
        {
            Trials = new Lazy<List<TrialData>>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    var Trials = AccessTools.Field(typeof(AllGameData), "trialDatas").GetValue(provider.GetAllGameData()) as List<TrialData>;
                    return Trials ?? [];
                }
                else
                {
                    return [];
                }
            });
            this.logger = logger;
        }

        public void Register(string key, TrialData item)
        {
            logger.Log(LogLevel.Info, $"Register Trial ({key})");
            Trials.Value.Add(item);
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. Trials.Value.Select(Trial => Trial.name)],
                RegisterIdentifierType.GUID => [.. Trials.Value.Select(Trial => Trial.GetID())],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out TrialData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = false;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var Trial in Trials.Value)
                    {
                        if (Trial.name == identifier)
                        {
                            lookup = Trial;
                            IsModded = this.ContainsKey(Trial.name);
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    foreach (var Trial in Trials.Value)
                    {
                        if (Trial.GetID() == identifier)
                        {
                            lookup = Trial;
                            IsModded = this.ContainsKey(Trial.name);
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
