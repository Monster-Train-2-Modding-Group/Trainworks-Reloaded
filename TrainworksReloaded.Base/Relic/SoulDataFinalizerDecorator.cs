using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class SoulDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<SoulDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RelicData>> cache;
        private readonly IRegister<ClassData> classRegister;
        private readonly IRegister<SoulPool> soulPoolRegister;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IDataFinalizer decoratee;

        private readonly FieldInfo SoulPoolRelicDataListField = AccessTools.Field(typeof(SoulPool), "relicDataList");
        private readonly FieldInfo LinkedClassField = AccessTools.Field(typeof(SoulData), "linkedClass");

        public SoulDataFinalizerDecorator(
            IModLogger<SoulDataFinalizerDecorator> logger,
            ICache<IDefinition<RelicData>> cache,
            IRegister<ClassData> classRegister,
            IRegister<SoulPool> soulPoolRegister,
            IRegister<Sprite> spriteRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.classRegister = classRegister;
            this.soulPoolRegister = soulPoolRegister;
            this.spriteRegister = spriteRegister;
            this.decoratee = decoratee;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRelicData((definition as RelicDataDefinition)!);
            }
            decoratee.FinalizeData();
            cache.Clear();
        }

        private void FinalizeRelicData(RelicDataDefinition definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var overrideMode = definition.Override;
            var key = definition.Key;
            var relicId = definition.Id.ToId(key, TemplateConstants.RelicData);

            if (data is not SoulData soul)
                return;

            if (definition.CopyData is not SoulData copyData)
                copyData = soul;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("soul").Exists())
                .Select(xs => xs.GetSection("soul"))
                .FirstOrDefault();

            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing SoulData {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            AccessTools.Field(typeof(SoulData), "soulTypeIcon").SetValue(data, copyData.GetSoulTypeIcon() ?? copyData.GetIcon());
            var iconConfig = configuration.GetSection("icon");
            var iconReference = iconConfig.ParseReference();
            if (
                iconReference != null
                && spriteRegister.TryLookupId(
                    iconReference.ToId(key, TemplateConstants.Sprite),
                    out var spriteLookup,
                    out var _,
                    iconReference.context
                )
            )
            {
                AccessTools.Field(typeof(SoulData), "soulTypeIcon").SetValue(data, spriteLookup);
            }
            else if (overrideMode == OverrideMode.Replace && iconReference == null && iconConfig.Exists())
            {
                AccessTools.Field(typeof(SoulData), "soulTypeIcon").SetValue(data, null);
            }

            var linkedClassReference = configuration.GetSection("class").ParseReference();
            var linkedClass = copyData.GetLinkedClass();
            if (linkedClassReference != null && classRegister.TryLookupName(linkedClassReference.ToId(key, TemplateConstants.Class), out var lookup, out var _, linkedClassReference.context))
            {
                linkedClass = lookup;
            }
            LinkedClassField.SetValue(soul, linkedClass);

            var poolReferences = configuration.GetSection("pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var poolReference in poolReferences)
            {
                var id = poolReference.ToId(key, TemplateConstants.SoulPool);
                if (soulPoolRegister.TryLookupId(id, out var pool, out var _, poolReference.context))
                {
                    var relicDataList = SoulPoolRelicDataListField.GetValue(pool) as ReorderableArray<SoulData>;
                    relicDataList?.Add(soul);
                    logger.Log(LogLevel.Debug, $"Added soul {definition.Id.ToId(key, TemplateConstants.RelicData)} to pool: {pool}");
                }
            }
        }
    }
}