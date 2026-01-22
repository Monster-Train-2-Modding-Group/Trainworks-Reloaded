using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class CollectableRelicDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<CollectableRelicDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RelicData>> cache;
        private readonly IRegister<ClassData> classRegister;
        private readonly IRegister<RelicPool> relicPoolRegister;
        private readonly IDataFinalizer decoratee;

        private readonly FieldInfo RelicPoolRelicDataListField = AccessTools.Field(typeof(RelicPool), "relicDataList");

        public CollectableRelicDataFinalizerDecorator(
            IModLogger<CollectableRelicDataFinalizerDecorator> logger,
            ICache<IDefinition<RelicData>> cache,
            IRegister<ClassData> classRegister,
            IRegister<RelicPool> relicPoolRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.classRegister = classRegister;
            this.relicPoolRegister = relicPoolRegister;
            this.decoratee = decoratee;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRelicData(definition);
            }
            decoratee.FinalizeData();
            cache.Clear();
        }

        private void FinalizeRelicData(IDefinition<RelicData> definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            var relicId = definition.Id.ToId(key, TemplateConstants.RelicData);

            if (data is not CollectableRelicData collectableRelic)
                return;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("collectable").Exists())
                .Select(xs => xs.GetSection("collectable"))
                .FirstOrDefault();
            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Collectable Relic Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            // Handle linked class
            var linkedClassReference = configuration.GetSection("class").ParseReference();
            if (linkedClassReference != null && classRegister.TryLookupName(linkedClassReference.ToId(key, TemplateConstants.Class), out var linkedClass, out var _, linkedClassReference.context))
            {
                AccessTools.Field(typeof(CollectableRelicData), "linkedClass").SetValue(collectableRelic, linkedClass);
            }

            var poolReferences = configuration.GetSection("pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var poolReference in poolReferences)
            {
                var id = poolReference.ToId(key, TemplateConstants.RelicPool);
                if (relicPoolRegister.TryLookupId(id, out var pool, out var _, poolReference.context))
                {
                    var relicDataList = RelicPoolRelicDataListField.GetValue(pool) as ReorderableArray<CollectableRelicData>;
                    relicDataList?.Add(collectableRelic);
                    logger.Log(LogLevel.Debug, $"Added relic {definition.Id.ToId(key, TemplateConstants.RelicData)} to pool: {pool}");
                }
            }
        }
    }
}