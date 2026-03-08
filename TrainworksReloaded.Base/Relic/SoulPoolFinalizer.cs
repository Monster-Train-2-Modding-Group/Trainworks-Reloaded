using HarmonyLib;
using Malee;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class SoulPoolFinalizer : IDataFinalizer
    {
        private readonly IModLogger<SoulPoolFinalizer> logger;
        private readonly ICache<IDefinition<SoulPool>> cache;
        private readonly IRegister<RelicData> relicRegister;

        public SoulPoolFinalizer(
            IModLogger<SoulPoolFinalizer> logger,
            ICache<IDefinition<SoulPool>> cache,
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

        private void FinalizePoolData(IDefinition<SoulPool> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing Soul Pool {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var soulDatas = new List<SoulData>();
            var soulReferences = configuration.GetSection("souls")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in soulReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RelicData);
                if (relicRegister.TryLookupName(id, out var relic, out var _, reference.context))
                {
                    if (relic is SoulData enhancer)
                    {
                        soulDatas.Add(enhancer);
                    }
                    else
                    {
                        logger.Log(LogLevel.Warning, $"RelicData {id} attempted to be added to SoulPool {data.name} but it is not a SoulData. Ignoring...");
                    }
                }
            }
            if (soulDatas.Count != 0)
            {
                var soulDataList =
                    (ReorderableArray<SoulData>)
                        AccessTools.Field(typeof(SoulPool), "relicDataList").GetValue(data);
                soulDataList.Clear();
                foreach (var item in soulDatas)
                {
                    soulDataList.Add(item);
                }
                AccessTools.Field(typeof(EnhancerPool), "relicDataList").SetValue(data, soulDataList);
            }
        }
    }
}
