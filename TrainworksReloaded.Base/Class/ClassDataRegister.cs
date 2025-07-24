using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Class
{
    public class ClassDataRegister : Dictionary<string, ClassData>, IRegister<ClassData>
    {
        private readonly Lazy<SaveManager> SaveManager;
        private readonly IModLogger<ClassDataRegister> logger;

        public ClassDataRegister(GameDataClient client, IModLogger<ClassDataRegister> logger)
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

        public void Register(string key, ClassData item)
        {
            logger.Log(LogLevel.Info, $"Register Clan {key}...");
            var gamedata = SaveManager.Value.GetAllGameData();
            var ClassDatas =
                (List<ClassData>)
                    AccessTools.Field(typeof(AllGameData), "classDatas").GetValue(gamedata);
            ClassDatas.Add(item);
            this.Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. SaveManager.Value.GetAllGameData().GetAllClassDatas().Select(classData => classData.name)],
                RegisterIdentifierType.GUID => [.. SaveManager.Value.GetAllGameData().GetAllClassDatas().Select(classData => classData.GetID())],
                _ => []
            };
        }

        public bool TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out ClassData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            lookup = null;
            IsModded = null;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    foreach (var @class in SaveManager.Value.GetAllGameData().GetAllClassDatas())
                    {
                        if (@class.name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = @class;
                            IsModded = ContainsKey(@class.name);
                            return true;
                        }
                    }
                    return false;
                case RegisterIdentifierType.GUID:
                    foreach (var @class in SaveManager.Value.GetAllGameData().GetAllClassDatas())
                    {
                        if (@class.GetID().Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            lookup = @class;
                            IsModded = ContainsKey(@class.name);
                            return true;
                        }
                    }
                    return false;
            }
            return false;
        }
    }
}
