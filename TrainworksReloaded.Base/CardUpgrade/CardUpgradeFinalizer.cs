using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CardUpgradeFinalizer> logger;
        private readonly ICache<IDefinition<CardUpgradeData>> cache;
        private readonly IRegister<CardData> cardRegister;
        private readonly IRegister<CharacterTriggerData> triggerRegister;
        private readonly IRegister<RoomModifierData> roomModifierRegister;
        private readonly IRegister<CardTraitData> traitRegister;
        private readonly IRegister<CardTriggerEffectData> cardTriggerRegister;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<CardUpgradeMaskData> upgradeMaskRegister;

        public CardUpgradeFinalizer(
            IModLogger<CardUpgradeFinalizer> logger,
            ICache<IDefinition<CardUpgradeData>> cache,
            IRegister<CardData> cardRegister,
            IRegister<CharacterTriggerData> triggerRegister,
            IRegister<RoomModifierData> roomModifierRegister,
            IRegister<CardTraitData> traitRegister,
            IRegister<CardTriggerEffectData> cardTriggerRegister,
            IRegister<CardUpgradeData> upgradeRegister,
            IRegister<VfxAtLoc> vfxRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<Sprite> spriteRegister,
            IRegister<CardUpgradeMaskData> upgradeMaskRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.cardRegister = cardRegister;
            this.triggerRegister = triggerRegister;
            this.roomModifierRegister = roomModifierRegister;
            this.traitRegister = traitRegister;
            this.cardTriggerRegister = cardTriggerRegister;
            this.upgradeRegister = upgradeRegister;
            this.vfxRegister = vfxRegister;
            this.statusRegister = statusRegister;
            this.spriteRegister = spriteRegister;
            this.upgradeMaskRegister = upgradeMaskRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCardUpgradeData(definition);
            }
            cache.Clear();
        }

        private void FinalizeCardUpgradeData(IDefinition<CardUpgradeData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();
            var cloneId = configuration.GetSection("clone_id").Value;
            var newlyCreatedContent = cloneId != null || overrideMode.IsNewContent();

            logger.Log(LogLevel.Debug, $"Finalizing Upgrade {data.name}...");

            //handle traits
            //traits do not default properly, if null, set to empty
            var traitDataUpgrades = data.GetTraitDataUpgrades() ?? [];
            var traitDataUpgradeConfig = configuration.GetSection("trait_upgrades");
            if (overrideMode == OverrideMode.Replace && traitDataUpgradeConfig.Exists())
            {
                traitDataUpgrades.Clear();
            }
            var traitDataUpgradesReference = traitDataUpgradeConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var traitReference in traitDataUpgradesReference)
            {
                var id = traitReference.ToId(key, TemplateConstants.Trait);
                if (traitRegister.TryLookupId(id, out var trait, out var _))
                {
                    traitDataUpgrades.Add(trait);
                }
            }
            AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").SetValue(data, traitDataUpgrades);

            //handle triggers
            var triggerUpgrades = data.GetCharacterTriggerUpgrades();
            var triggerUpgradeConfig = configuration.GetSection("trigger_upgrades");
            if (overrideMode == OverrideMode.Replace && triggerUpgradeConfig.Exists())
            {
                triggerUpgrades.Clear();
            }
            var triggerUpgradeReferences = triggerUpgradeConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var triggerReference in triggerUpgradeReferences)
            {
                var id = triggerReference.ToId(key, TemplateConstants.CharacterTrigger);
                if (triggerRegister.TryLookupId(id, out var trigger, out var _))
                {
                    triggerUpgrades.Add(trigger);
                }
            }
            AccessTools.Field(typeof(CardUpgradeData), "triggerUpgrades").SetValue(data, triggerUpgrades);

            //handle card triggers
            var cardTriggerUpgrades = data.GetCardTriggerUpgrades();
            var cardTriggerConfig = configuration.GetSection("card_trigger_upgrades");
            if (overrideMode == OverrideMode.Replace && cardTriggerConfig.Exists())
            {
                cardTriggerUpgrades.Clear();
            }
            var cardTriggerUpgradeReferences = cardTriggerConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var cardTriggerReference in cardTriggerUpgradeReferences)
            {
                var id = cardTriggerReference.ToId(key, TemplateConstants.CardTrigger);
                if (cardTriggerRegister.TryLookupId(id, out var trigger, out var _))
                {
                    cardTriggerUpgrades.Add(trigger);
                }
            }
            AccessTools.Field(typeof(CardUpgradeData), "cardTriggerUpgrades").SetValue(data, cardTriggerUpgrades);

            //handle roomModifiers
            var roomModifierUpgrades = data.GetRoomModifierUpgrades();
            var roomModifierConfig = configuration.GetSection("room_modifier_upgrades");
            if (overrideMode == OverrideMode.Replace && roomModifierConfig.Exists())
            {
                roomModifierUpgrades.Clear();
            }
            var roomModifierUpgradeReferences = roomModifierConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var roomModifierReference in roomModifierUpgradeReferences)
            {
                var id = roomModifierReference.ToId(key, TemplateConstants.RoomModifier);
                if (roomModifierRegister.TryLookupId(id, out var roomModifier, out var _))
                {
                    roomModifierUpgrades.Add(roomModifier);
                }
            }
            AccessTools.Field(typeof(CardUpgradeData), "roomModifierUpgrades").SetValue(data, roomModifierUpgrades);

            //status
            var statusEffectUpgrades = data.GetStatusEffectUpgrades() ?? [];
            var statusEffectUpgradesConfig = configuration.GetSection("status_effect_upgrades");
            if (overrideMode == OverrideMode.Replace && statusEffectUpgradesConfig.Exists())
            {
                statusEffectUpgrades.Clear();
            }
            foreach (var child in statusEffectUpgradesConfig.GetChildren())
            {
                var statusReference = child.GetSection("status").ParseReference();
                if (statusReference == null)
                    continue;

                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                // Make sure the status effect exists. If using @ notation.
                // else it is a vanilla status effect.
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    statusEffectUpgrades.Add(new StatusEffectStackData
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child.GetSection("count").ParseInt() ?? 0,
                        fromPermanentUpgrade = child.GetSection("from_permanent_upgrade").ParseBool() ?? false
                    });
                }
            }
            AccessTools
                .Field(typeof(CardUpgradeData), "statusEffectUpgrades")
                .SetValue(data, statusEffectUpgrades);

            //handle upgrades to remove
            var upgradesToRemove = data.GetUpgradesToRemove();
            var upgradesToRemoveConfig = configuration.GetSection("remove_upgrades");
            if (overrideMode == OverrideMode.Replace && upgradesToRemoveConfig.Exists())
            {
                upgradesToRemove.Clear();
            }
            var upgradesToRemoveReferences = upgradesToRemoveConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var removeUpgradeReference in upgradesToRemoveReferences)
            {
                var id = removeUpgradeReference.ToId(key, TemplateConstants.Upgrade);
                if (upgradeRegister.TryLookupName(id, out var upgrade, out var _))
                {
                    upgradesToRemove.Add(upgrade);
                }
            }
            AccessTools.Field(typeof(CardUpgradeData), "upgradesToRemove").SetValue(data, upgradesToRemove);

            var appliedVFXId = configuration.GetSection("applied_vfx").ParseReference()?.ToId(key, TemplateConstants.Vfx);
            if (newlyCreatedContent || appliedVFXId != null)
            {
                vfxRegister.TryLookupId(appliedVFXId ?? "", out var applied_vfx, out var _);
                AccessTools
                    .Field(typeof(CardUpgradeData), "appliedVFX")
                    .SetValue(data, applied_vfx);
            }

            var removedVFXId = configuration.GetSection("removed_vfx").ParseReference()?.ToId(key, TemplateConstants.Vfx);
            if (newlyCreatedContent || removedVFXId != null)
            {
                vfxRegister.TryLookupId(removedVFXId ?? "", out var removed_vfx, out var _);
                AccessTools
                    .Field(typeof(CardUpgradeData), "removedVFX")
                    .SetValue(data, removed_vfx);
            }

            var persistentVFXId = configuration.GetSection("persistent_vfx").ParseReference()?.ToId(key, TemplateConstants.Vfx);
            if (newlyCreatedContent || persistentVFXId != null)
            {
                vfxRegister.TryLookupId(persistentVFXId ?? "", out var persistent_vfx, out var _);
                AccessTools
                    .Field(typeof(CardUpgradeData), "persistentVFX")
                    .SetValue(data, persistent_vfx);
            }

            List<CardUpgradeMaskData> filters = data.GetFilters();
            var filterConfig = configuration.GetSection("filters");
            if (overrideMode == OverrideMode.Replace && filterConfig.Exists())
            {
                filters.Clear();
            }
            var filterReferences = filterConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in filterReferences)
            {
                var id = reference.ToId(key, TemplateConstants.UpgradeMask);
                if (upgradeMaskRegister.TryLookupId(id, out var lookup, out var _))
                {
                    filters.Add(lookup);
                }
            }
            AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(data, filters);

            var abilityConfig = configuration.GetSection("ability_upgrade");
            var abilityReference = abilityConfig.ParseReference();
            if (abilityReference != null)
            {
                cardRegister.TryLookupName(abilityReference.ToId(key, TemplateConstants.Card), out var abilityCard, out var _);
                AccessTools.Field(typeof(CardUpgradeData), "unitAbilityUpgrade").SetValue(data, abilityCard);
            }
            if (overrideMode == OverrideMode.Replace && abilityReference == null && abilityConfig.Exists())
            {
                AccessTools.Field(typeof(CardUpgradeData), "unitAbilityUpgrade").SetValue(data, null);
            }

            // Setting an icon already set to null isn't useful so not supporting it.
            var spriteReference = configuration.GetSection("icon").ParseReference();
            if (spriteReference != null)
            {
                spriteRegister.TryLookupId(spriteReference.ToId(key, TemplateConstants.Sprite), out var spriteLookup, out var _);
                AccessTools.Field(typeof(CardUpgradeData), "upgradeIcon").SetValue(data, spriteLookup);
            }
        }
    }
}
