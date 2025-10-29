using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Prefab
{
    public class AdditionalTooltipFinalizer : IDataFinalizer
    {
        private readonly IModLogger<AdditionalTooltipFinalizer> logger;
        private readonly ICache<IDefinition<AdditionalTooltipData>> cache;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;

        public AdditionalTooltipFinalizer(
            IModLogger<AdditionalTooltipFinalizer> logger,
            ICache<IDefinition<AdditionalTooltipData>> cache,
            IRegister<StatusEffectData> statusRegister,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.statusRegister = statusRegister;
            this.triggerEnumRegister = triggerEnumRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeItem(definition);
            }
            cache.Clear();
        }

        public void FinalizeItem(IDefinition<AdditionalTooltipData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Info, $"Finalizing AdditionalTooltipData {key} {definition.Id} path: {configuration.GetPath()}...");

            data.isTriggerTooltip = false;
            var triggerReference = configuration.GetSection("trigger").ParseReference();
            if (triggerReference != null)
            {
                var triggerId = triggerReference.ToId(key, TemplateConstants.CharacterTriggerEnum);
                if (triggerEnumRegister.TryLookupId(triggerId, out var triggerFound, out var _, triggerReference.context))
                {
                    data.isTriggerTooltip = true;
                    data.trigger = triggerFound;
                }
            }

            data.isStatusTooltip = false;
            var statusReference = configuration.GetSection("status").ParseReference();
            if (statusReference != null)
            {
                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _, statusReference.context))
                {
                    data.isStatusTooltip = true;
                    data.statusId = statusEffectData.GetStatusId();
                }
            }

        }
    }
}
