using HarmonyLib;
using Malee;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class RelicPoolFinalizer : IDataFinalizer
    {
        private readonly IModLogger<RelicPoolFinalizer> logger;
        private readonly ICache<IDefinition<RelicPool>> cache;
        private readonly IRegister<RelicData> relicRegister;

        public RelicPoolFinalizer(
            IModLogger<RelicPoolFinalizer> logger,
            ICache<IDefinition<RelicPool>> cache,
            IRegister<RelicData> relicRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.relicRegister = relicRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizePoolData(definition);
            }
            cache.Clear();
        }

        private void FinalizePoolData(IDefinition<RelicPool> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing Relic Pool {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var relicDatas = new List<CollectableRelicData>();
            var relicReferences = configuration.GetSection("relics")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in relicReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RelicData);
                if (relicRegister.TryLookupName(id, out var relic, out var _, reference.context))
                {
                    if (relic is CollectableRelicData collectable)
                    {
                        relicDatas.Add(collectable);
                    }
                    else
                    {
                        logger.Log(LogLevel.Warning, $"RelicData {id} attempted to be added to RelicPool {data.name} but it is not a CollectableRelic. Ignoring...");
                    }
                }
            }
            if (relicDatas.Count != 0)
            {
                var relicDataList =
                    (ReorderableArray<CollectableRelicData>)
                        AccessTools.Field(typeof(RelicPool), "relicDataList").GetValue(data);
                relicDataList.Clear();
                foreach (var item in relicDatas)
                {
                    relicDataList.Add(item);
                }
                AccessTools.Field(typeof(RelicPool), "relicDataList").SetValue(data, relicDataList);
            }
        }
    }
}
