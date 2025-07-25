using HarmonyLib;
using Malee;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Card
{
    public class CardDataRegister : Dictionary<string, CardData>, IRegister<CardData>
    {
        private readonly Lazy<SaveManager> SaveManager;
        private readonly IModLogger<CardDataRegister> logger;
        public CardPool CustomCardPool;
        public ReorderableArray<CardData> CardPoolBacking;

        public CardDataRegister(GameDataClient client, IModLogger<CardDataRegister> logger)
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
            CustomCardPool = ScriptableObject.CreateInstance<CardPool>();
            CustomCardPool.name = "ModdedPool";
            CardPoolBacking =
                (ReorderableArray<CardData>)
                    AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(CustomCardPool);
            this.logger = logger;
        }

        public void Register(string key, CardData item)
        {
            logger.Log(LogLevel.Info, $"Register Card {key}... ");
            CardPoolBacking.Add(item);
            var gamedata = SaveManager.Value.GetAllGameData();
            var CardDatas =
                (List<CardData>)
                    AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(gamedata);
            CardDatas.Add(item);
            Add(key, item);
        }

        public bool TryLookupId(
            string id,
            [NotNullWhen(true)] out CardData? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            lookup = null;
            IsModded = null;
            foreach (var card in SaveManager.Value.GetAllGameData().GetAllCardData())
            {
                if (card.GetID().Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    lookup = card;
                    IsModded = this.ContainsKey(card.name);
                    return true;
                }
            }
            return false;
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. SaveManager.Value.GetAllGameData().GetAllCardData().Select(card => card.GetAssetKey())],
                RegisterIdentifierType.GUID => [.. SaveManager.Value.GetAllGameData().GetAllCardData().Select(card => card.GetID())],
                _ => [],
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out CardData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = null;
            foreach (var card in SaveManager.Value.GetAllGameData().GetAllCardData())
            {
                switch (identifierType)
                {
                    case RegisterIdentifierType.ReadableID:
                        if (card.GetAssetKey().Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = card;
                            IsModded = this.ContainsKey(card.name);
                            return true;
                        }
                        break;
                    case RegisterIdentifierType.GUID:
                        if (card.GetID().Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = card;
                            IsModded = this.ContainsKey(card.name);
                            return true;
                        }
                        break;
                }
            }
            return false;
        }
    }
}
