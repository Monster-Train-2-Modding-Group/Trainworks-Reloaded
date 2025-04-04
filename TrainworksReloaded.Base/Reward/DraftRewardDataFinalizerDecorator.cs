using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Reward
{
    public class DraftRewardDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<DraftRewardDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RewardData>> cache;
        private readonly IRegister<CardPool> cardPoolRegister;
        private readonly IRegister<ClassData> classRegister;
        private readonly IDataFinalizer decoratee;

        public DraftRewardDataFinalizerDecorator(
            IModLogger<DraftRewardDataFinalizerDecorator> logger,
            ICache<IDefinition<RewardData>> cache,
            IRegister<CardPool> cardPoolRegister,
            IRegister<ClassData> classRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.cardPoolRegister = cardPoolRegister;
            this.classRegister = classRegister;
            this.decoratee = decoratee;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRewardData(definition);
            }
            decoratee.FinalizeData();
            cache.Clear();
        }

        /// <summary>
        /// Finalize Draft Reward Definitions
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeRewardData(IDefinition<RewardData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            if (data is not DraftRewardData draftData)
                return;

            var draftConfiguration = configuration
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("draft").Exists())
                .Select(xs => xs.GetSection("draft"))
                .First();
            if (draftConfiguration == null)
                return;

            logger.Log(
                Core.Interfaces.LogLevel.Info,
                $"Finalizing Draft Reward Data {definition.Id.ToId(key, TemplateConstants.RewardData)}... "
            );

            // Set draft pool
            var draftPool = draftConfiguration.GetSection("draft_pool").ParseString();
            if (
                draftPool != null
                && cardPoolRegister.TryLookupId(
                    draftPool.ToId(key, TemplateConstants.CardPool),
                    out var cardPoolData,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(DraftRewardData), "draftPool")
                    .SetValue(draftData, cardPoolData);
            }

            // Set class type
            var classType = draftConfiguration.GetSection("class_type").ParseClassType() ?? RunState.ClassType.None;
            AccessTools.Field(typeof(DraftRewardData), "classType").SetValue(draftData, classType);

            // Set draft options count
            var draftOptionsCount = draftConfiguration.GetSection("draft_options_count").ParseInt() ?? 2;
            AccessTools.Field(typeof(DraftRewardData), "draftOptionsCount").SetValue(draftData, (uint)Math.Abs(draftOptionsCount));

            // Set extra copies
            var extraCopies = draftConfiguration.GetSection("extra_copies").ParseInt() ?? 0;
            AccessTools.Field(typeof(DraftRewardData), "extraCopies").SetValue(draftData, extraCopies);

            // Set boolean flags
            var disableSkip = draftConfiguration.GetSection("disable_skip").ParseBool() ?? false;
            AccessTools.Field(typeof(DraftRewardData), "disableSkip").SetValue(draftData, disableSkip);

            var ignoreRelicRarityOverride = draftConfiguration.GetSection("ignore_relic_rarity_override").ParseBool() ?? false;
            AccessTools.Field(typeof(DraftRewardData), "ignoreRelicRarityOverride").SetValue(draftData, ignoreRelicRarityOverride);

            var useRunRarityFloors = draftConfiguration.GetSection("use_run_rarity_floors").ParseBool() ?? false;
            AccessTools.Field(typeof(DraftRewardData), "useRunRarityFloors").SetValue(draftData, useRunRarityFloors);


            var flattenRarityForDraftRate = draftConfiguration.GetSection("flatten_rarity_for_draft_rate").ParseBool() ?? false;
            AccessTools.Field(typeof(DraftRewardData), "flattenRarityForDraftRate").SetValue(draftData, flattenRarityForDraftRate);


            var grantSingleCard = draftConfiguration.GetSection("grant_single_card").ParseBool() ?? false;
            AccessTools.Field(typeof(DraftRewardData), "grantSingleCard").SetValue(draftData, grantSingleCard);


            var classTypeOverride = draftConfiguration.GetSection("class_type_override").ParseBool() ?? false;
            AccessTools.Field(typeof(DraftRewardData), "classTypeOverride").SetValue(draftData, classTypeOverride);


            var useDraftTicketOverrideValues = draftConfiguration.GetSection("use_draft_ticket_override_values").ParseBool() ?? false;
            AccessTools.Field(typeof(DraftRewardData), "useDraftTicketOverrideValues").SetValue(draftData, useDraftTicketOverrideValues);


            // Set rarity floor
            var rarityFloor = draftConfiguration.GetSection("rarity_floor").ParseRarity() ?? CollectableRarity.Common;
            AccessTools.Field(typeof(DraftRewardData), "rarityFloorOverride").SetValue(draftData, rarityFloor);

            // Set class data override
            var classDataOverride = draftConfiguration.GetSection("class_data_override").ParseString();
            if (classDataOverride != null && classRegister.TryLookupName(classDataOverride.ToId(key, TemplateConstants.Class), out var classData, out var _))
            {
                AccessTools.Field(typeof(DraftRewardData), "classDataOverride").SetValue(draftData, classData);
            }

            // Set rarity ticket values
            var cardRarityTicketValues = draftConfiguration
                .GetSection("card_rarity_ticket_values")
                .GetChildren()
                .Select(xs => new RarityTicket
                {
                    rarityType = xs.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common,
                    ticketValue = xs.GetSection("value").ParseInt() ?? 0
                })
                .ToList();
            AccessTools
                .Field(typeof(DraftRewardData), "cardRarityTicketValues")
                .SetValue(draftData, cardRarityTicketValues);

            var enhancerRarityTicketValues = draftConfiguration
                .GetSection("enhancer_rarity_ticket_values")
                .GetChildren()
                .Select(xs => new RarityTicket
                {
                    rarityType = xs.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common,
                    ticketValue = xs.GetSection("value").ParseInt() ?? 0
                })
                .ToList();
            AccessTools
                .Field(typeof(DraftRewardData), "enhancerRarityTicketValues")
                .SetValue(draftData, enhancerRarityTicketValues);

            var relicRarityTicketValues = draftConfiguration
                .GetSection("relic_rarity_ticket_values")
                .GetChildren()
                .Select(xs => new RarityTicket
                {
                    rarityType = xs.GetSection("rarity").ParseRarity() ?? CollectableRarity.Common,
                    ticketValue = xs.GetSection("value").ParseInt() ?? 0
                })
                .ToList();
            AccessTools
                .Field(typeof(DraftRewardData), "relicRarityTicketValues")
                .SetValue(draftData, relicRarityTicketValues);

            // Set GrantableRewardData fields
            var isServiceMerchantReward = draftConfiguration.GetSection("is_service_merchant_reward").ParseBool() ?? false;
            AccessTools.Field(typeof(GrantableRewardData), "_isServiceMerchantReward").SetValue(draftData, isServiceMerchantReward);

            var merchantServiceIndex = draftConfiguration.GetSection("merchant_service_index").ParseInt() ?? 0;
            AccessTools.Field(typeof(GrantableRewardData), "_merchantServiceIndex").SetValue(draftData, merchantServiceIndex);

            var applyTrialDataModifiers = draftConfiguration.GetSection("apply_trial_data_modifiers").ParseBool() ?? false;
            AccessTools.Field(typeof(GrantableRewardData), "_applyTrialDataModifiers").SetValue(draftData, applyTrialDataModifiers);
        }
    }
}