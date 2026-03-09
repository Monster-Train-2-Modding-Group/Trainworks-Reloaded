using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static ShinyShoe.Audio.CoreSoundEffectData;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Challenges
{
    public class ChallengeDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<ChallengeDataFinalizer> logger;
        private readonly ICache<IDefinition<SpChallengeData>> cache;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<ClassData> classRegister;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<CharacterData> characterRegister;

        private readonly FieldInfo MutatorsField = AccessTools.Field(typeof(SpChallengeData), "mutators");

        public ChallengeDataFinalizer(
            IModLogger<ChallengeDataFinalizer> logger,
            ICache<IDefinition<SpChallengeData>> cache,
            IRegister<Sprite> spriteRegister,
            IRegister<ClassData> classDataRegister,
            IRegister<RelicData> relicDataRegister,
            IRegister<CharacterData> characterDataRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
            this.classRegister = classDataRegister;
            this.relicRegister = relicDataRegister;
            this.characterRegister = characterDataRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeChallengeData(definition);
            }
            cache.Clear();
        }

        private void FinalizeChallengeData(IDefinition<SpChallengeData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();

            logger.Log(LogLevel.Info, $"Finalizing Challenge {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var mainClanConfig = configuration.GetSection("main_clan");
            var mainClanReference = mainClanConfig.ParseReference();
            if (mainClanReference != null)
            {
                classRegister.TryLookupName(
                    mainClanReference.ToId(key, TemplateConstants.Class),
                    out var lookup,
                    out var _,
                    mainClanReference.context);
                AccessTools.Field(typeof(SpChallengeData), "mainClan").SetValue(data, lookup);
            }
            else if (overrideMode == OverrideMode.Replace && mainClanReference == null && mainClanConfig.Exists())
            {
                AccessTools.Field(typeof(SpChallengeData), "mainClan").SetValue(data, null);
            }

            var alliedClanConfig = configuration.GetSection("allied_clan");
            var alliedClanReference = alliedClanConfig.ParseReference();
            if (alliedClanReference != null)
            {
                classRegister.TryLookupName(
                    alliedClanReference.ToId(key, TemplateConstants.Class),
                    out var lookup,
                    out var _,
                    alliedClanReference.context);
                AccessTools.Field(typeof(SpChallengeData), "alliedClan").SetValue(data, lookup);
            }
            else if (overrideMode == OverrideMode.Replace && alliedClanReference == null && alliedClanConfig.Exists())
            {
                AccessTools.Field(typeof(SpChallengeData), "alliedClan").SetValue(data, null);
            }

            var iconConfig = configuration.GetSection("icon");
            var iconReference = iconConfig.ParseReference();
            if (iconReference != null)
            {
                spriteRegister.TryLookupName(
                    iconReference.ToId(key, TemplateConstants.Sprite),
                    out var lookup,
                    out var _,
                    iconReference.context);
                AccessTools.Field(typeof(SpChallengeData), "icon").SetValue(data, lookup);
            }
            else if (overrideMode == OverrideMode.Replace && iconReference == null && iconConfig.Exists())
            {
                AccessTools.Field(typeof(SpChallengeData), "icon").SetValue(data, null);
            }

            var pyreConfig = configuration.GetSection("pyre_heart");
            var pyreReference = pyreConfig.ParseReference();
            if (pyreReference != null)
            {
                characterRegister.TryLookupName(
                    pyreReference.ToId(key, TemplateConstants.Character),
                    out var lookup,
                    out var _,
                    pyreReference.context);
                AccessTools.Field(typeof(SpChallengeData), "pyreHeartCharacterData").SetValue(data, lookup);
            }
            else if (overrideMode == OverrideMode.Replace && pyreReference == null && pyreConfig.Exists())
            {
                AccessTools.Field(typeof(SpChallengeData), "pyreHeartCharacterData").SetValue(data, null);
            }

            var mutators = MutatorsField.GetValue(data) as List<MutatorData> ?? [];
            var mutatorConfig = configuration.GetSection("mutators");
            if (overrideMode == OverrideMode.Replace && mutatorConfig.Exists())
            {
                mutators.Clear();
            }
            var relicReferences = mutatorConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in relicReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RelicData);
                if (relicRegister.TryLookupName(id, out var relicData, out var _, reference.context))
                {
                    if (relicData is not MutatorData mutator)
                    {
                        logger.Log(LogLevel.Warning, $"Attempt to add non-Mutator RelicData {relicData.name} to Challenge {definition.Id}. Ignoring...");
                        continue;
                    }    
                    mutators.Add(mutator);
                }
            }
            MutatorsField.SetValue(data, mutators);
        }
    }
}
