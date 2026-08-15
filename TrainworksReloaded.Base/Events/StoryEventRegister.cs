using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Events
{
    public class StoryEventRegister : Dictionary<string, StoryEventData>, IRegister<StoryEventData>
    {
        private Lazy<SaveManager> SaveManager;
        private readonly IModLogger<StoryEventRegister> logger;

        private static readonly System.Reflection.FieldInfo StoryEventDatasField = AccessTools.Field(typeof(AllGameData), "storyEventDatas");

        public StoryEventRegister(GameDataClient client, IModLogger<StoryEventRegister> logger)
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

        List<string> IRegister<StoryEventData>.GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return identifierType switch
            {
                RegisterIdentifierType.ReadableID => [.. this.Keys],
                RegisterIdentifierType.GUID => [.. this.Keys],
                _ => []
            };
        }

        void IRegisterableDictionary<StoryEventData>.Register(string key, StoryEventData item)
        {
            logger.Log(LogLevel.Info, $"Register StoryEventData ({key})");
            
            Add(key, item);
            var events = StoryEventDatasField.GetValue(SaveManager.Value.GetAllGameData()) as List<StoryEventData>;
            events!.Add(item);
        }

        bool IRegister<StoryEventData>.TryLookupIdentifier(string identifier, RegisterIdentifierType identifierType, [NotNullWhen(true)] out StoryEventData? lookup, [NotNullWhen(true)] out bool? IsModded)
        {
            IsModded = true;
            switch (identifierType)
            {
                case RegisterIdentifierType.ReadableID:
                    return this.TryGetValue(identifier, out lookup);
                case RegisterIdentifierType.GUID:
                    return this.TryGetValue(identifier, out lookup);
                default:
                    lookup = null;
                    return false;
            }
        }
    }
}
