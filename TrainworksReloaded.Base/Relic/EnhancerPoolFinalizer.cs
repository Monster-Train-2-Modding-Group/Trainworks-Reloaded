using HarmonyLib;
using Malee;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerPoolFinalizer : IDataFinalizer
    {
        private readonly IModLogger<EnhancerPoolFinalizer> logger;
        private readonly ICache<IDefinition<EnhancerPool>> cache;
        private readonly IRegister<RelicData> relicRegister;

        public EnhancerPoolFinalizer(
            IModLogger<EnhancerPoolFinalizer> logger,
            ICache<IDefinition<EnhancerPool>> cache,
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

        private void FinalizePoolData(IDefinition<EnhancerPool> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Debug, $"Finalizing Enhancer Pool {data.name}... ");

            var enhancerDatas = new List<EnhancerData>();
            var enhancerReferences = configuration.GetSection("enhancers")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in enhancerReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RelicData);
                if (relicRegister.TryLookupName(id, out var relic, out var _))
                {
                    if (relic is EnhancerData enhancer)
                    {
                        enhancerDatas.Add(enhancer);
                    }
                    else
                    {
                        logger.Log(LogLevel.Warning, $"RelicData {id} attempted to be added to EnhancerPool {data.name} but it is not a EnhancerData. Ignoring...");
                    }
                }
            }
            if (enhancerDatas.Count != 0)
            {
                var enhancerDataList =
                    (ReorderableArray<EnhancerData>)
                        AccessTools.Field(typeof(EnhancerPool), "relicDataList").GetValue(data);
                enhancerDataList.Clear();
                foreach (var item in enhancerDatas)
                {
                    enhancerDataList.Add(item);
                }
                AccessTools.Field(typeof(EnhancerPool), "relicDataList").SetValue(data, enhancerDataList);
            }
        }
    }
}
