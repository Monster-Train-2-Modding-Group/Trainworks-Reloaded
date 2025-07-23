using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine.UIElements;

namespace TrainworksReloaded.Base.Scenarios
{
    public class BackgroundRegister : Dictionary<string, BackgroundData>, IRegister<BackgroundData>
    {
        private readonly IModLogger<BackgroundRegister> logger;
        private readonly Lazy<List<BackgroundData>> Backgrounds;

        public BackgroundRegister(GameDataClient client, IModLogger<BackgroundRegister> logger)
        {
            Backgrounds = new Lazy<List<BackgroundData>>(() =>
            {
                if (client.TryGetProvider<SaveManager>(out var provider))
                {
                    var backgrounds = AccessTools.Field(typeof(AllGameData), "backgroundDatas").GetValue(provider.GetAllGameData()) as List<BackgroundData>;
                    return backgrounds ?? [];
                }
                else
                {
                    return [];
                }
            });
            this.logger = logger;
        }

        public void Register(string key, BackgroundData item)
        {
            logger.Log(LogLevel.Debug, $"Register Background ({key})");
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

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out BackgroundData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        return true;
                    }
                    foreach (var background in Backgrounds.Value)
                    {
                        if (background.GetAssetKey() == identifier)
                        {
                            IsModded = false;
                            lookup = background;
                            return true;
                        }
                    }
                    lookup = GetDefault();
                    IsModded = false;
                    return lookup != null;
                case RegisterIdentifierType.GUID:
                    if (this.TryGetValue(identifier, out lookup))
                    {
                        return true;
                    }
                    foreach (var background in Backgrounds.Value)
                    {
                        if (background.GetAssetKey() == identifier)
                        {
                            IsModded = false;
                            lookup = background;
                            return true;
                        }
                    }
                    lookup = GetDefault();
                    IsModded = false;
                    return lookup != null;
                default:
                    return false;
            }
        }

        private BackgroundData? GetDefault()
        {
            foreach (var background in Backgrounds.Value)
            {
                if (background.GetAssetKey() == "CaveBackgroundData")
                {
                    return background;
                }
            }
            return null;
        }
    }
}