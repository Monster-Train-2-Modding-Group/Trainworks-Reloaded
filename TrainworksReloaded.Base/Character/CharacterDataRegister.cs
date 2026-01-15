using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Character
{
    public class CharacterDataRegister : Dictionary<string, CharacterData>, IRegister<CharacterData>
    {
        private readonly Lazy<SaveManager> SaveManager;
        private readonly IModLogger<CharacterDataRegister> logger;

        public CharacterDataRegister(
            GameDataClient client,
            IModLogger<CharacterDataRegister> logger
        )
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


        public void Register(string key, CharacterData item)
        {
            logger.Log(LogLevel.Info, $"Register Character {key}...");
            var gamedata = SaveManager.Value.GetAllGameData();
            var CharacterDatas =
                (List<CharacterData>)
                    AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(gamedata);
            CharacterDatas.Add(item);
            if (item.IsPyreHeart())
            {
                gamedata.GetBalanceData().GetPyreHeartCharacterDatas().Add(item);
            }
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. SaveManager.Value.GetAllGameData().GetAllCharacterData().Select(character => character.GetAssetKey())],
                RegisterIdentifierType.GUID => [.. SaveManager.Value.GetAllGameData().GetAllCharacterData().Select(character => character.GetID())],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CharacterData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = null;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var card in SaveManager.Value.GetAllGameData().GetAllCharacterData())
                    {
                        if (card.GetAssetKey().Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = card;
                            IsModded = this.ContainsKey(card.name);
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    foreach (var card in SaveManager.Value.GetAllGameData().GetAllCharacterData())
                    {
                        if (card.GetID().Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = card;
                            IsModded = this.ContainsKey(card.name);
                            return true;
                        }
                    }
                    return false;
            }
            return false;
        }

    }
}
