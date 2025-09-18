using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeMaskFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CardUpgradeMaskFinalizer> logger;
        private readonly ICache<IDefinition<CardUpgradeMaskData>> cache;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<ClassData> classRegister;
        private readonly IRegister<CardPool> poolRegister;
        private readonly IRegister<SubtypeData> subtypeRegister;

        public CardUpgradeMaskFinalizer(
            IModLogger<CardUpgradeMaskFinalizer> logger,
            ICache<IDefinition<CardUpgradeMaskData>> cache,
            IRegister<CardUpgradeData> upgradeRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<ClassData> classRegister,
            IRegister<CardPool> poolRegister,
            IRegister<SubtypeData> subtypeRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.upgradeRegister = upgradeRegister;
            this.statusRegister = statusRegister;
            this.classRegister = classRegister;
            this.poolRegister = poolRegister;
            this.subtypeRegister = subtypeRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCardUpgradeMask(definition);
            }
            cache.Clear();
        }

        private void FinalizeCardUpgradeMask(IDefinition<CardUpgradeMaskData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Debug, $"Finalizing Upgrade Mask {data.name}...");

            List<ClassData> requiredClasses = [];
            var classReferences = configuration.GetSection("required_class")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in classReferences)
            {
                if (classRegister.TryLookupName(reference.ToId(key, TemplateConstants.Class), out var lookup, out var _))
                {
                    requiredClasses.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredLinkedClans").SetValue(data, requiredClasses);

            List<ClassData> excludedClasses = [];
            var excludedClassReferences = configuration.GetSection("excluded_class")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in excludedClassReferences)
            {
                if (classRegister.TryLookupName(reference.ToId(key, TemplateConstants.Class), out var lookup, out var _))
                {
                    excludedClasses.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedLinkedClans").SetValue(data, excludedClasses);

            List<StatusEffectStackData> requiredStatus = [];
            foreach (var child in configuration.GetSection("required_status").GetChildren())
            {
                var statusReference = child.GetSection("status").ParseReference();
                if (statusReference == null)
                    continue;
                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    requiredStatus.Add(new StatusEffectStackData
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child.GetSection("count").ParseInt() ?? 0,
                        fromPermanentUpgrade = child.GetSection("from_permanent_upgrade").ParseBool() ?? false
                    });
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredStatusEffects").SetValue(data, requiredStatus);

            List<StatusEffectStackData> excludedStatus = [];
            foreach (var child in configuration.GetSection("excluded_status").GetChildren())
            {
                var statusReference = child.GetSection("status").ParseReference();
                if (statusReference == null)
                    continue;
                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    excludedStatus.Add(new StatusEffectStackData
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child.GetSection("count").ParseInt() ?? 0,
                        fromPermanentUpgrade = child.GetSection("from_permanent_upgrade").ParseBool() ?? false
                    });
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedStatusEffects").SetValue(data, excludedStatus);

            List<CardPool> allowedPools = [];
            var allowedPoolReferences = configuration.GetSection("allowed_pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var poolReference in allowedPoolReferences)
            {
                if (poolRegister.TryLookupId(poolReference.ToId(key, TemplateConstants.CardPool), out var lookup, out var _))
                {
                    allowedPools.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "allowedCardPools").SetValue(data, allowedPools);

            List<CardPool> disallowedPools = [];
            var disallowedPoolReferences = configuration.GetSection("disallowed_pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var poolReference in disallowedPoolReferences)
            {
                if (poolRegister.TryLookupId(poolReference.ToId(key, TemplateConstants.CardPool), out var lookup, out var _))
                {
                    disallowedPools.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "disallowedCardPools").SetValue(data, disallowedPools);

            List<CardUpgradeData> requiredUpgrades = [];
            var requiredUpgradeReferences = configuration.GetSection("required_upgrade")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var upgradeReference in requiredUpgradeReferences)
            {
                if (upgradeRegister.TryLookupName(upgradeReference.ToId(key, TemplateConstants.Upgrade), out var lookup, out var _))
                {
                    requiredUpgrades.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredCardUpgrades").SetValue(data, requiredUpgrades);

            List<CardUpgradeData> excludedUpgrades = [];
            var excludedUpgradeReferences = configuration.GetSection("excluded_upgrade")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var upgradeReferences in excludedUpgradeReferences)
            {
                if (upgradeRegister.TryLookupName(upgradeReferences.ToId(key, TemplateConstants.Upgrade), out var lookup, out var _))
                {
                    excludedUpgrades.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedCardUpgrades").SetValue(data, excludedUpgrades);

            List<string> subtypesRequired = [];
            var requiredSubtypesReferences = configuration.GetSection("required_subtypes")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var subtypeReference in requiredSubtypesReferences)
            {
                if (subtypeRegister.TryLookupId(subtypeReference.ToId(key, TemplateConstants.Subtype), out var lookup, out var _))
                {
                    subtypesRequired.Add(lookup.Key);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "requiredSubtypes").SetValue(data, subtypesRequired);

            List<string> subtypesExcluded = [];
            var excludedSubtypesReferences = configuration.GetSection("excluded_subtypes")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var subtypeReference in excludedSubtypesReferences)
            {
                if (subtypeRegister.TryLookupId(subtypeReference.ToId(key, TemplateConstants.Subtype), out var lookup, out var _))
                {
                    subtypesExcluded.Add(lookup.Key);
                }
            }
            AccessTools.Field(typeof(CardUpgradeMaskData), "excludedSubtypes").SetValue(data, subtypesExcluded);
        }
    }
}
