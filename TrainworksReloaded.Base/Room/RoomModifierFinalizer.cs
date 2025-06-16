using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Room
{
    public class RoomModifierFinalizer : IDataFinalizer
    {
        private readonly IModLogger<RoomModifierFinalizer> logger;
        private readonly ICache<IDefinition<RoomModifierData>> cache;
        private readonly IRegister<AdditionalTooltipData> tooltipRegister;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<CardData> cardDataRegister;
        private readonly IRegister<CardUpgradeData> upgradeDataRegister;
        private readonly IRegister<CardEffectData> cardEffectDataRegister;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<StatusEffectData> statusRegister;
        private readonly IRegister<CharacterTriggerData.Trigger> triggerEnumRegister;
        private readonly IRegister<SubtypeData> subtypeRegister;

        public RoomModifierFinalizer(
            IModLogger<RoomModifierFinalizer> logger,
            ICache<IDefinition<RoomModifierData>> cache,
            IRegister<AdditionalTooltipData> tooltipRegister,
            IRegister<Sprite> spriteRegister,
            IRegister<CardData> cardDataRegister,
            IRegister<CardUpgradeData> upgradeDataRegister,
            IRegister<CardEffectData> cardEffectDataRegister,
            IRegister<VfxAtLoc> vfxRegister,
            IRegister<StatusEffectData> statusRegister,
            IRegister<CharacterTriggerData.Trigger> triggerEnumRegister,
            IRegister<SubtypeData> subtypeRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.tooltipRegister = tooltipRegister;
            this.spriteRegister = spriteRegister;
            this.cardDataRegister = cardDataRegister;
            this.upgradeDataRegister = upgradeDataRegister;
            this.cardEffectDataRegister = cardEffectDataRegister;
            this.vfxRegister = vfxRegister;
            this.statusRegister = statusRegister;
            this.triggerEnumRegister = triggerEnumRegister;
            this.subtypeRegister = subtypeRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRoomModifier(definition);
            }
            cache.Clear();
        }

        /// <summary>
        /// Finalize RoomModifier Definition
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeRoomModifier(IDefinition<RoomModifierData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;

            logger.Log(LogLevel.Debug, 
                $"Finalizing Room Modifier {definition.Id.ToId(key, "RoomModifier")}... "
            );

            var cardReference = configuration.GetSection("param_card").ParseReference();
            if (
                cardReference != null
                && cardDataRegister.TryLookupName(
                    cardReference.ToId(key, TemplateConstants.Card),
                    out var cardData,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "paramCardData")
                    .SetValue(data, cardData);
            }

            var upgradeReference = configuration.GetSection("param_upgrade").ParseReference();
            if (
                upgradeReference != null
                && upgradeDataRegister.TryLookupName(
                    upgradeReference.ToId(key, TemplateConstants.Upgrade),
                    out var upgradeLookup,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "paramCardUpgardeData")
                    .SetValue(data, upgradeLookup);
            }

            var triggeredVFXId = configuration.GetSection("triggered_vfx").ParseReference()?.ToId(key, TemplateConstants.Vfx);
            if (
                triggeredVFXId != null
                && vfxRegister.TryLookupId(
                    triggeredVFXId,
                    out var vfxLookup,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(RoomModifierData), "triggeredVFX")
                    .SetValue(data, vfxLookup);
            }

            var cardEffectDatas = new List<CardEffectData>();
            var effectReferences = configuration.GetDeprecatedSection("effects", "param_effects")
               .GetChildren()
               .Select(x => x.ParseReference())
               .Where(x => x != null)
               .Cast<ReferencedObject>();
            foreach (var reference in effectReferences)
            {
                var id = reference.ToId(key, TemplateConstants.Effect);
                if (cardEffectDataRegister.TryLookupId(id, out var card, out var _))
                {
                    cardEffectDatas.Add(card);
                }
            }

            if (cardEffectDatas.Count != 0)
                AccessTools
                    .Field(typeof(RoomModifierData), "paramCardEffects")
                    .SetValue(data, cardEffectDatas);

            // Status Effects
            List<StatusEffectStackData> paramStatusEffects = [];
            foreach (var child in configuration.GetSection("param_status_effects").GetChildren())
            {
                var statusReference = child.GetSection("status").ParseReference();
                if (statusReference == null)
                    continue;
                var statusEffectId = statusReference.ToId(key, TemplateConstants.StatusEffect);
                if (statusRegister.TryLookupId(statusEffectId, out var statusEffectData, out var _))
                {
                    paramStatusEffects.Add(new StatusEffectStackData()
                    {
                        statusId = statusEffectData.GetStatusId(),
                        count = child?.GetSection("count").ParseInt() ?? 0,
                    });
                }
            }
            AccessTools
                .Field(typeof(RoomModifierData), "paramStatusEffects")
                .SetValue(data, paramStatusEffects.ToArray());

            //trigger
            var paramTrigger = CharacterTriggerData.Trigger.OnDeath;
            var triggerReference = configuration.GetDeprecatedSection("trigger", "param_trigger").ParseReference();
            if (triggerReference != null)
            {
                if (
                    !triggerEnumRegister.TryLookupId(
                        triggerReference.ToId(key, TemplateConstants.CharacterTriggerEnum),
                        out var triggerFound,
                        out var _
                    )
                )
                {
                    paramTrigger = triggerFound;
                }
            }
            AccessTools
                .Field(typeof(RoomModifierData), "paramTrigger")
                .SetValue(data, paramTrigger);

            var paramSubtype = "SubtypesData_None";
            var subtypeReference = configuration.GetSection("param_subtype").ParseReference();
            if (subtypeReference != null)
            {
                if (subtypeRegister.TryLookupId(
                    subtypeReference.ToId(key, TemplateConstants.Subtype),
                    out var lookup,
                    out var _))
                {
                    paramSubtype = lookup.Key;
                }
            }
            AccessTools
                .Field(typeof(RoomModifierData), "paramSubtype")
                .SetValue(data, paramSubtype);

            var tooltips = new List<AdditionalTooltipData>();
            var tooltipReferences = configuration
                .GetSection("additional_tooltips")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in tooltipReferences)
            {
                var id = reference.ToId(key, TemplateConstants.AdditionalTooltip);
                if (tooltipRegister.TryLookupName(id, out var tooltip, out var _))
                {
                    tooltips.Add(tooltip);
                }
            }
            AccessTools
                .Field(typeof(RoomModifierData), "additionalTooltips")
                .SetValue(data, tooltips.ToArray());
        }
    }
}
