using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TrainworksReloaded.Base.Character;
using TrainworksReloaded.Base.Effect;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.CardUpgrade
{
    public class CardUpgradeFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CardUpgradeFinalizer> logger;
        private readonly ICache<IDefinition<CardUpgradeData>> cache;
        private readonly IRegister<CharacterTriggerData> triggerRegister;
        private readonly IRegister<RoomModifierData> roomModifierRegister;
        private readonly IRegister<CardTraitData> traitRegister;
        private readonly IRegister<CardTriggerEffectData> cardTriggerRegister;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<CardUpgradeMaskData> upgradeMaskRegister;

        public CardUpgradeFinalizer(
            IModLogger<CardUpgradeFinalizer> logger,
            ICache<IDefinition<CardUpgradeData>> cache,
            IRegister<CharacterTriggerData> triggerRegister,
            IRegister<RoomModifierData> roomModifierRegister,
            IRegister<CardTraitData> traitRegister,
            IRegister<CardTriggerEffectData> cardTriggerRegister,
            IRegister<CardUpgradeData> upgradeRegister,
            IRegister<VfxAtLoc> vfxRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<CardUpgradeMaskData> upgradeMaskRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.triggerRegister = triggerRegister;
            this.roomModifierRegister = roomModifierRegister;
            this.traitRegister = traitRegister;
            this.cardTriggerRegister = cardTriggerRegister;
            this.upgradeRegister = upgradeRegister;
            this.vfxRegister = vfxRegister;
            this.statusRegister = statusRegister;
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

            logger.Log(Core.Interfaces.LogLevel.Info, $"Finalizing Upgrade {data.name}... ");

            //handle traits
            //traits do not default properly, if null, set to empty
            var traitDataUpgradesVal =
                (List<CardTraitData>)
                    AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").GetValue(data);
            if (traitDataUpgradesVal == null)
                AccessTools
                    .Field(typeof(CardUpgradeData), "traitDataUpgrades")
                    .SetValue(data, new List<CardTraitData>());

            var traitDataUpgrades = new List<CardTraitData>();
            var traitDataUpgradesConfig = configuration.GetSection("trait_upgrades").GetChildren();
            foreach (var traitDataUpgrade in traitDataUpgradesConfig)
            {
                if (traitDataUpgrade == null)
                {
                    continue;
                }
                var idConfig = traitDataUpgrade.GetSection("id").Value;
                if (idConfig == null)
                {
                    continue;
                }

                var id = idConfig.ToId(key, TemplateConstants.Trait);
                if (traitRegister.TryLookupId(id, out var card, out var _))
                {
                    traitDataUpgrades.Add(card);
                }
            }
            if (traitDataUpgrades.Count != 0)
                AccessTools
                    .Field(typeof(CardUpgradeData), "traitDataUpgrades")
                    .SetValue(data, traitDataUpgrades);

            //handle triggers
            var triggerUpgrades = new List<CharacterTriggerData>();
            var triggerUpgradesConfig = configuration.GetSection("trigger_upgrades").GetChildren();
            foreach (var triggerUpgrade in triggerUpgradesConfig)
            {
                if (triggerUpgrade == null)
                {
                    continue;
                }
                var idConfig = triggerUpgrade.GetSection("id").Value;
                if (idConfig == null)
                {
                    continue;
                }

                var id = idConfig.ToId(key, TemplateConstants.CharacterTrigger);
                if (triggerRegister.TryLookupId(id, out var card, out var _))
                {
                    triggerUpgrades.Add(card);
                }
            }
            if (triggerUpgrades.Count != 0)
                AccessTools
                    .Field(typeof(CardUpgradeData), "triggerUpgrades")
                    .SetValue(data, triggerUpgrades);

            //handle card triggers
            var cardTriggerUpgrades = new List<CardTriggerEffectData>();
            var cardTriggerUpgradesConfig = configuration
                .GetSection("card_trigger_upgrades")
                .GetChildren();
            foreach (var cardTriggerUpdate in cardTriggerUpgradesConfig)
            {
                if (cardTriggerUpdate == null)
                {
                    continue;
                }
                var idConfig = cardTriggerUpdate.GetSection("id").Value;
                if (idConfig == null)
                {
                    continue;
                }

                var id = idConfig.ToId(key, TemplateConstants.CardTrigger);
                if (cardTriggerRegister.TryLookupId(id, out var card, out var _))
                {
                    cardTriggerUpgrades.Add(card);
                }
            }
            if (cardTriggerUpgrades.Count != 0)
                AccessTools
                    .Field(typeof(CardUpgradeData), "cardTriggerUpgrades")
                    .SetValue(data, cardTriggerUpgrades);

            //handle roomModifiers
            var roomModifierUpgrades = new List<RoomModifierData>();
            var roomModifierUpgradesConfig = configuration
                .GetSection("room_modifier_upgrades")
                .GetChildren();
            foreach (var roomModifier in roomModifierUpgradesConfig)
            {
                if (roomModifier == null)
                {
                    continue;
                }
                var idConfig = roomModifier.GetSection("id").Value;
                if (idConfig == null)
                {
                    continue;
                }
                var id = idConfig.ToId(key, TemplateConstants.RoomModifier);
                if (roomModifierRegister.TryLookupId(id, out var card, out var _))
                {
                    roomModifierUpgrades.Add(card);
                }
            }

            if (roomModifierUpgrades.Count != 0)
                AccessTools
                    .Field(typeof(CardUpgradeData), "roomModifierUpgrades")
                    .SetValue(data, roomModifierUpgrades);

            //status
            var statusEffectUpgrades = new List<StatusEffectStackData>();
            var statusEffectUpgradesConfig = configuration.GetSection("status_effect_upgrades").GetChildren();
            foreach (var child in statusEffectUpgradesConfig)
            {
                var idConfig = child?.GetSection("status").Value;
                if (idConfig == null)
                    continue;
                var statusEffectId = idConfig.ToId(key, TemplateConstants.StatusEffect);
                string statusId = idConfig;
                // Make sure the status effect exists. If using @ notation.
                // else it is a vanilla status effect.
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    statusId = statusEffectData.GetStatusId();
                }
                statusEffectUpgrades.Add(new StatusEffectStackData()
                {
                    statusId = statusId,
                    count = child?.GetSection("count").ParseInt() ?? 0,
                });
            }
            AccessTools
                .Field(typeof(CardUpgradeData), "statusEffectUpgrades")
                .SetValue(data, statusEffectUpgrades);

            //handle upgrades to remove
            var upgradesToRemove = new List<CardUpgradeData>();
            var upgradesToRemoveConfig = configuration.GetSection("remove_upgrades").GetChildren();
            foreach (var removeUpgrade in upgradesToRemoveConfig)
            {
                if (removeUpgrade == null)
                {
                    continue;
                }
                var idConfig = removeUpgrade.GetSection("id").Value;
                if (idConfig == null)
                {
                    continue;
                }
                var id = idConfig.ToId(key, TemplateConstants.Upgrade);
                if (upgradeRegister.TryLookupId(id, out var card, out var _))
                {
                    upgradesToRemove.Add(card);
                }
            }

            if (upgradesToRemove.Count != 0)
                AccessTools
                    .Field(typeof(CardUpgradeData), "upgradesToRemove")
                    .SetValue(data, upgradesToRemove);

            var appliedVFX = configuration.GetSection("applied_vfx").ParseString() ?? "";
            if (
                vfxRegister.TryLookupId(
                    appliedVFX.ToId(key, TemplateConstants.Vfx),
                    out var applied_vfx,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(CardUpgradeData), "appliedVFX")
                    .SetValue(data, applied_vfx);
            }

            var removedVFX = configuration.GetSection("removed_vfx").ParseString() ?? "";
            if (
                vfxRegister.TryLookupId(
                    removedVFX.ToId(key, TemplateConstants.Vfx),
                    out var removed_vfx,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(CardUpgradeData), "removedVFX")
                    .SetValue(data, removed_vfx);
            }

            var persistentVFX = configuration.GetSection("persistent_vfx").ParseString() ?? "";
            if (
                vfxRegister.TryLookupId(
                    persistentVFX.ToId(key, TemplateConstants.Vfx),
                    out var persistent_vfx,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(CardUpgradeData), "persistentVFX")
                    .SetValue(data, persistent_vfx);
            }

            List<CardUpgradeMaskData> filters = [];
            foreach (var child in configuration.GetSection("filters").GetChildren())
            {
                var id = child?.GetSection("id").ParseString();
                if (id == null ||
                    !upgradeMaskRegister.TryLookupId(
                        id.ToId(key, TemplateConstants.UpgradeMask),
                        out var lookup,
                        out var _))
                {
                    continue;
                }
                filters.Add(lookup);
            }
            AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(data, filters);
        }
    }
}
