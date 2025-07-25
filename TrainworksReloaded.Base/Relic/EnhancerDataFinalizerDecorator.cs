using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class EnhancerDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<EnhancerDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RelicData>> cache;
        private readonly IRegister<ClassData> classRegister;
        private readonly IDataFinalizer decoratee;

        public EnhancerDataFinalizerDecorator(
            IModLogger<EnhancerDataFinalizerDecorator> logger,
            ICache<IDefinition<RelicData>> cache,
            IRegister<ClassData> classRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.classRegister = classRegister;
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

            if (data is not EnhancerData enhancer)
                return;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("enhancer").Exists())
                .Select(xs => xs.GetSection("enhancer"))
                .FirstOrDefault();
            if (configuration == null)
                return;

            logger.Log(LogLevel.Debug, $"Finalizing Enhancer Data {relicId}...");

            // Handle linked class
            var linkedClassReference = configuration.GetSection("class").ParseReference();
            if (linkedClassReference != null && classRegister.TryLookupName(linkedClassReference.ToId(key, TemplateConstants.Class), out var linkedClass, out var _))
            {
                AccessTools.Field(typeof(EnhancerData), "linkedClass").SetValue(enhancer, linkedClass);
            }
        }
    }
}