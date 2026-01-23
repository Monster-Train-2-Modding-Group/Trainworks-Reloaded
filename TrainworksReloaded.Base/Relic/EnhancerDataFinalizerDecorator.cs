using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<EnhancerDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RelicData>> cache;
        private readonly IRegister<ClassData> classRegister;
        private readonly IRegister<EnhancerPool> enhancerPoolRegister;
        private readonly IDataFinalizer decoratee;

        private readonly FieldInfo EnhancerPoolRelicDataListField = AccessTools.Field(typeof(EnhancerPool), "relicDataList");
        private readonly FieldInfo LinkedClassField = AccessTools.Field(typeof(EnhancerData), "linkedClass");

        public EnhancerDataFinalizerDecorator(
            IModLogger<EnhancerDataFinalizerDecorator> logger,
            ICache<IDefinition<RelicData>> cache,
            IRegister<ClassData> classRegister,
            IRegister<EnhancerPool> enhancerPoolRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.classRegister = classRegister;
            this.enhancerPoolRegister = enhancerPoolRegister;
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

            if (data is not EnhancerData enhancer)
                return;

            if (definition.CopyData is not EnhancerData copyData)
                copyData = enhancer;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("enhancer").Exists())
                .Select(xs => xs.GetSection("enhancer"))
                .FirstOrDefault();

            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Enhancer Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            // Handle linked class
            var linkedClassReference = configuration.GetSection("class").ParseReference();
            var linkedClass = copyData.GetLinkedClass();
            if (linkedClassReference != null && classRegister.TryLookupName(linkedClassReference.ToId(key, TemplateConstants.Class), out var lookup, out var _, linkedClassReference.context))
            {
                linkedClass = lookup;
            }
            LinkedClassField.SetValue(enhancer, linkedClass);

            var poolReferences = configuration.GetSection("pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var poolReference in poolReferences)
            {
                var id = poolReference.ToId(key, TemplateConstants.RelicPool);
                if (enhancerPoolRegister.TryLookupId(id, out var pool, out var _, poolReference.context))
                {
                    var relicDataList = EnhancerPoolRelicDataListField.GetValue(pool) as ReorderableArray<EnhancerData>;
                    relicDataList?.Add(enhancer);
                    logger.Log(LogLevel.Debug, $"Added enhancer {definition.Id.ToId(key, TemplateConstants.RelicData)} to pool: {pool}");
                }
            }
        }
    }
}