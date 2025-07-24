using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Map
{
    public class MapNodeRegister : Dictionary<string, MapNodeData>, IRegister<MapNodeData>
    {
        private readonly IModLogger<MapNodeRegister> logger;
        private readonly Lazy<SaveManager> SaveManager;

        public MapNodeRegister(GameDataClient client, IModLogger<MapNodeRegister> logger)
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


        public void Register(string key, MapNodeData item)
        {
            logger.Log(LogLevel.Info, $"Register Map Node {key}...");
            var gamedata = SaveManager.Value.GetAllGameData();
            var MapNodeDatas =
                (List<MapNodeData>)
                    AccessTools.Field(typeof(AllGameData), "mapNodeDatas").GetValue(gamedata);
            MapNodeDatas.Add(item);
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            var allGameData = SaveManager.Value.GetAllGameData();
            var mapData =
                (List<MapNodeData>)
                    AccessTools.Field(typeof(AllGameData), "mapNodeDatas").GetValue(allGameData);
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    if (allGameData != null)
                    {
                        return [.. mapData.Select(map => map.name)];
                    }
                    return [.. this.Select(map => map.Key)];
                case RegisterIdentifierType.GUID:
                    if (allGameData != null)
                    {
                        return [.. mapData.Select(map => map.GetID())];
                    }
                    return [.. this.Select(map => map.Value.GetID())];
                default:
                    return [];
            }
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out MapNodeData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    var allGameData = SaveManager.Value.GetAllGameData();
                    var mapData =
                        (List<MapNodeData>)
                            AccessTools.Field(typeof(AllGameData), "mapNodeDatas").GetValue(allGameData);
                    if (allGameData != null)
                    {
                        foreach (var map in mapData)
                        {
                            if (map.name == identifier)
                            {
                                lookup = map;
                                IsModded = this.ContainsKey(allGameData.name);
                                return true;
                            }
                        }
                    }
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    var mapNodeData = SaveManager.Value.GetAllGameData().FindMapNodeData(identifier);
                    if (mapNodeData != null)
                    {
                        lookup = mapNodeData;
                        IsModded = this.ContainsKey(mapNodeData.name);
                        return true;
                    }
                    return this.TryGetValue(identifier, out lookup);
                default:
                    return false;
            }
        }
    }
}
