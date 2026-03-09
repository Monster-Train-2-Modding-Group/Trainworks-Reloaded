using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Challenges
{
    public class ChallengeDataRegister : Dictionary<string, SpChallengeData>, IRegister<SpChallengeData>
    {
        private readonly Lazy<SaveManager> SaveManager;
        private readonly IModLogger<ChallengeDataRegister> logger;

        public ChallengeDataRegister(GameDataClient client, IModLogger<ChallengeDataRegister> logger)
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

        public void Register(string key, SpChallengeData item)
        {
            logger.Log(LogLevel.Info, $"Register Challenge {key}...");
            var gamedata = SaveManager.Value.GetAllGameData();
            var spChallengeDatas =
                (List<SpChallengeData>)
                    AccessTools.Field(typeof(AllGameData), "spChallengeDatas").GetValue(gamedata);
            spChallengeDatas.Add(item);
            this.Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. SaveManager.Value.GetAllGameData().GetAllSpChallengeDatas().Select(spChallengeData => spChallengeData.name)],
                RegisterIdentifierType.GUID => [.. SaveManager.Value.GetAllGameData().GetAllSpChallengeDatas().Select(spChallengeData => spChallengeData.GetID())],
                _ => []
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out SpChallengeData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = null;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var challenge in SaveManager.Value.GetAllGameData().GetAllSpChallengeDatas())
                    {
                        if (challenge.name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = challenge;
                            IsModded = ContainsKey(challenge.name);
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    foreach (var challenge in SaveManager.Value.GetAllGameData().GetAllSpChallengeDatas())
                    {
                        if (challenge.GetID().Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = challenge;
                            IsModded = ContainsKey(challenge.name);
                            return true;
                        }
                    }
                    return false;
            }
            return false;
        }
    }
}
