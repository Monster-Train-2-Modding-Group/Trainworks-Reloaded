using HarmonyLib;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Scenarios
{
    public class TrialDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<TrialDataFinalizer> logger;
        private readonly ICache<IDefinition<TrialData>> cache;
        private readonly IRegister<RelicData> relicRegister;
        private readonly IRegister<RewardData> rewardRegister;
        private readonly IRegister<TrialDataList> trialListRegister;

        public TrialDataFinalizer(IModLogger<TrialDataFinalizer> logger, ICache<IDefinition<TrialData>> cache, IRegister<RelicData> relicRegister, IRegister<RewardData> rewardRegister, IRegister<TrialDataList> trialListRegister)
        {
            this.logger = logger;
            this.cache = cache;
            this.relicRegister = relicRegister;
            this.rewardRegister = rewardRegister;
            this.trialListRegister = trialListRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeTrial(definition);
            }
            cache.Clear();
        }

        public void FinalizeTrial(IDefinition<TrialData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing Trial {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            var sinReference = configuration.GetSection("sin").ParseReference();
            if (sinReference != null)
            {
                var id = sinReference.ToId(key, TemplateConstants.RelicData);
                relicRegister.TryLookupId(id, out var lookup, out var _, sinReference.context);
                if (lookup is not SinsData)
                {
                    logger.Log(LogLevel.Warning, $"Relic found is not a SinsData relic. Behavior may not be as expected.");
                }
                AccessTools.Field(typeof(TrialData), "sin").SetValue(data, lookup);
            }

            var rewardList = data.RewardList;
            var rewardReferences = configuration.GetSection("rewards")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in rewardReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RewardData);
                rewardRegister.TryLookupName(id, out var lookup, out var _, reference.context);
                if (lookup != null)
                {
                    rewardList.Add(lookup);
                }
            }

            var trialListReferences = configuration.GetSection("pools")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in trialListReferences)
            {
                var id = reference.ToId(key, TemplateConstants.TrialList);
                trialListRegister.TryLookupId(id, out var lookup, out var _, reference.context);
                if (lookup != null)
                {
                    var trialsArray = lookup.TrialsData ?? [];
                    var newTrials = trialsArray.Append(data);
                    AccessTools.Field(typeof(TrialDataList), "trialDatas").SetValue(lookup, newTrials.ToArray());
                }
            }
        }
    }
}
