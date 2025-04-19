using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Effect
{
    public class CardEffectFinalizer : IDataFinalizer
    {
        private readonly IRegister<CharacterData> characterDataRegister;
        private readonly IRegister<CardUpgradeData> cardUpgradeRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;
        private readonly IRegister<CardUpgradeMaskData> upgradeMaskRegister;
        private readonly IRegister<CardPool> cardPoolRegister;
        private readonly ICache<IDefinition<CardEffectData>> cache;

        public CardEffectFinalizer(
            IRegister<CharacterData> characterDataRegister,
            IRegister<CardUpgradeData> cardUpgradeRegister,
            IRegister<CardUpgradeMaskData> upgradeMaskRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister,
            IRegister<CardPool> cardPoolRegister,
            ICache<IDefinition<CardEffectData>> cache
        )
        {
            this.characterDataRegister = characterDataRegister;
            this.cardUpgradeRegister = cardUpgradeRegister;
            this.statusRegister = statusRegister;
            this.triggerEnumRegister = triggerEnumRegister;
            this.upgradeMaskRegister = upgradeMaskRegister;
            this.cardPoolRegister = cardPoolRegister;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCardEffectData(definition);
            }
            cache.Clear();
        }

        private void FinalizeCardEffectData(IDefinition<CardEffectData> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var data = definition.Data;

            var characterConfig = configuration.GetSection("param_character_data").Value;
            if (
                characterConfig != null
                && characterDataRegister.TryLookupName(
                    characterConfig.ToId(key, "Character"),
                    out var characterData,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(CardEffectData), "paramCharacterData")
                    .SetValue(data, characterData);
            }

            var characterConfig2 = configuration.GetSection("param_character_data_2").Value;
            if (
                characterConfig2 != null
                && characterDataRegister.TryLookupName(
                    characterConfig2.ToId(key, "Character"),
                    out var characterData2,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(CardEffectData), "paramAdditionalCharacterData")
                    .SetValue(data, characterData2);
            }

            //card pools
            var characterDataPool = new List<CharacterData>();
            var characterDataPoolConfig = configuration
                .GetSection("param_character_data_pool")
                .GetChildren()
                .Select(x => x.Value);
            foreach (var characterDataConfig in characterDataPoolConfig)
            {
                if (
                    characterDataConfig != null
                    && characterDataRegister.TryLookupName(
                        characterDataConfig.ToId(key, "Character"),
                        out var card,
                        out var _
                    )
                )
                {
                    characterDataPool.Add(card);
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "paramCharacterDataPool")
                .SetValue(data, characterDataPool);

            //upgrades
            var upgradeConfig = configuration.GetSection("param_upgrade").Value;
            if (
                upgradeConfig != null
                && cardUpgradeRegister.TryLookupName(
                    upgradeConfig.ToId(key, TemplateConstants.Upgrade),
                    out var upgradeData,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(CardEffectData), "paramCardUpgradeData")
                    .SetValue(data, upgradeData);
            }

            // Status effects.
            var statusEffectStackMultiplier = configuration.GetSection("status_effect_multiplier").ParseString() ?? "";
            if (!statusEffectStackMultiplier.IsNullOrEmpty())
            {
                var statusEffectId = statusEffectStackMultiplier.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    statusEffectStackMultiplier = statusEffectData.GetStatusId();
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "statusEffectStackMultiplier")
                .SetValue(data, statusEffectStackMultiplier);


            //string[]
            var targetModeStatusEffectsFilterConfig = configuration.GetSection("status_effect_filters").GetChildren();
            List<String> targetModeStatusEffectsFilter = [];
            foreach (var child in targetModeStatusEffectsFilterConfig)
            {
                var idConfig = child?.GetSection("status").Value;
                if (idConfig == null)
                    continue;
                var statusEffectId = idConfig.ToId(key, TemplateConstants.StatusEffect);
                string statusId = idConfig;
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    statusId = statusEffectData.GetStatusId();
                }
                targetModeStatusEffectsFilter.Add(statusId);
            }
            AccessTools
                .Field(typeof(CardEffectData), "targetModeStatusEffectsFilter")
                .SetValue(data, targetModeStatusEffectsFilter.ToArray());

            List<StatusEffectStackData> paramStatusEffects = [];
            foreach (var child in configuration.GetSection("param_status_effects").GetChildren())
            {
                var idConfig = child?.GetSection("status").Value;
                if (idConfig == null)
                    continue;
                var statusEffectId = idConfig.ToId(key, TemplateConstants.StatusEffect);
                string statusId = idConfig;
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    statusId = statusEffectData.GetStatusId();
                }
                paramStatusEffects.Add(new StatusEffectStackData()
                {
                    statusId = statusId,
                    count = child?.GetSection("count").ParseInt() ?? 0,
                });
            }
            AccessTools
                .Field(typeof(CardEffectData), "paramStatusEffects")
                .SetValue(data, paramStatusEffects.ToArray());

            //trigger
            var paramTrigger = CharacterTriggerData.Trigger.OnDeath;
            var triggerSection = configuration.GetSection("trigger");
            if (triggerSection.Value != null)
            {
                var value = triggerSection.Value;
                if (
                    !triggerEnumRegister.TryLookupId(
                        value.ToId(key, TemplateConstants.CharacterTriggerEnum),
                        out var triggerFound,
                        out var _
                    )
                )
                {
                    paramTrigger = triggerFound;
                }
                else
                {
                    paramTrigger = triggerSection.ParseTrigger() ?? default;
                }
            }
            AccessTools
                .Field(typeof(CardEffectData), "paramTrigger")
                .SetValue(data, paramTrigger);

            var filterId = configuration.GetSection("param_card_filter")?.ParseString();
            if (filterId != null)
            {
                upgradeMaskRegister.TryLookupId(
                    filterId.ToId(key, TemplateConstants.UpgradeMask), 
                    out var lookup, 
                    out var _);
                AccessTools.Field(typeof(CardEffectData), "paramCardFilter").SetValue(data, lookup);
            }

            var cardPoolId = configuration.GetSection("param_card_pool")?.ParseString();
            if (cardPoolId != null)
            {
                cardPoolRegister.TryLookupId(
                    cardPoolId.ToId(key, TemplateConstants.CardPool),
                    out var lookup,
                    out var _
                    );
                AccessTools.Field(typeof(CardEffectData), "paramCardPool").SetValue(data, lookup);
            }
        }
    }
}
