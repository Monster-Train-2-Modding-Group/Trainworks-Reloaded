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

namespace TrainworksReloaded.Base.Class
{
    public class ClassDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<ClassDataFinalizer> logger;
        private readonly ICache<IDefinition<ClassData>> cache;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<CardData> cardDataRegister;
        private readonly IRegister<RelicData> relicDataRegister;
        private readonly IRegister<CardUpgradeData> upgradeDataRegister;
        private readonly IRegister<EnhancerPool> enhancerPoolRegister;

        public ClassDataFinalizer(
            IModLogger<ClassDataFinalizer> logger,
            ICache<IDefinition<ClassData>> cache,
            IRegister<Sprite> spriteRegister,
            IRegister<CardData> cardDataRegister,
            IRegister<RelicData> relicDataRegister,
            IRegister<CardUpgradeData> upgradeDataRegister,
            IRegister<EnhancerPool> enhancerPoolRegister
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
            this.cardDataRegister = cardDataRegister;
            this.relicDataRegister = relicDataRegister;
            this.upgradeDataRegister = upgradeDataRegister;
            this.enhancerPoolRegister = enhancerPoolRegister;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeClassData(definition);
            }
            cache.Clear();
        }

        /// <summary>
        /// Finalize Card Definitions
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeClassData(IDefinition<ClassData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();

            logger.Log(LogLevel.Debug, $"Finalizing Clan {data.name}...");

            var iconSet = AccessTools.Field(typeof(ClassData), "icons").GetValue(data);
            var iconSetType = typeof(ClassData).GetNestedType(
                "IconSet",
                System.Reflection.BindingFlags.NonPublic
            );
            if (iconSet == null)
            {
                iconSet = iconSetType.GetConstructor([]).Invoke([]);
                AccessTools.Field(typeof(ClassData), "icons").SetValue(data, iconSet);
            }
            var iconField = configuration.GetSection("icons");

            var smallIcon = iconField.GetSection("small").ParseReference();
            if (
                smallIcon != null
                && spriteRegister.TryLookupName(
                    smallIcon.ToId(key, TemplateConstants.Sprite),
                    out var lookup,
                    out var _
                )
            )
            {
                AccessTools.Field(iconSetType, "small").SetValue(iconSet, lookup);
            }

            var mediumIcon = iconField.GetSection("medium").ParseReference();
            if (
                mediumIcon != null
                && spriteRegister.TryLookupName(
                    mediumIcon.ToId(key, TemplateConstants.Sprite),
                    out var lookup2,
                    out var _
                )
            )
            {
                AccessTools.Field(iconSetType, "medium").SetValue(iconSet, lookup2);
            }

            var largeIcon = iconField.GetSection("large").ParseReference();
            if (
                largeIcon != null
                && spriteRegister.TryLookupName(
                    largeIcon.ToId(key, TemplateConstants.Sprite),
                    out var lookup3,
                    out var _
                )
            )
            {
                AccessTools.Field(iconSetType, "large").SetValue(iconSet, lookup3);
            }

            var silhouette = iconField.GetSection("silhouette").ParseReference();
            if (
                silhouette != null
                && spriteRegister.TryLookupName(
                    silhouette.ToId(key, TemplateConstants.Sprite),
                    out var lookup4,
                    out var _
                )
            )
            {
                AccessTools.Field(iconSetType, "silhouette").SetValue(iconSet, lookup4);
            }

            //handle starter relics
            var starterRelics = data.GetStarterRelics() ?? [];
            var relicConfig = configuration.GetSection("starter_relics");
            if (overrideMode == OverrideMode.Replace && relicConfig.Exists())
            {
                starterRelics.Clear();
            }
            var relicReferences = relicConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var reference in relicReferences)
            {
                var id = reference.ToId(key, TemplateConstants.RelicData);
                if (relicDataRegister.TryLookupName(id, out var relicData, out var _))
                {
                    starterRelics.Add(relicData);
                }
            }
            AccessTools.Field(typeof(ClassData), "starterRelics").SetValue(data, starterRelics);

            //handle starter card upgrade
            var upgradeConfig = configuration.GetDeprecatedSection("starter_upgrade", "starter_card_upgrade");
            var starterUpgradeReference = configuration.GetDeprecatedSection("starter_upgrade", "starter_card_upgrade").ParseReference();
            if (
                starterUpgradeReference != null
                && upgradeDataRegister.TryLookupName(
                    starterUpgradeReference.ToId(key, TemplateConstants.Upgrade),
                    out var upgradeData,
                    out var _
                )
            )
            {
                AccessTools
                    .Field(typeof(ClassData), "starterCardUpgrade")
                    .SetValue(data, upgradeData);
            }
            if (overrideMode == OverrideMode.Replace && starterUpgradeReference == null && upgradeConfig.Exists())
            {
                AccessTools
                    .Field(typeof(ClassData), "starterCardUpgrade")
                    .SetValue(data, null);
            }

            var enhancerPoolConfig = configuration.GetSection("random_draft_enhancer_pool");
            var enhancerPoolReference = enhancerPoolConfig.ParseReference();
            if (enhancerPoolReference != null)
            {
                if (
                    enhancerPoolRegister.TryLookupId(
                        enhancerPoolReference.ToId(key, TemplateConstants.EnhancerPool),
                        out var enhancerPool,
                        out var _
                    )
                )
                {
                    AccessTools
                        .Field(typeof(ClassData), "randomDraftEnhancerPool")
                        .SetValue(data, enhancerPool);
                }
            }
            if (overrideMode == OverrideMode.Replace && enhancerPoolReference == null && enhancerPoolConfig.Exists())
            {
                AccessTools
                    .Field(typeof(ClassData), "randomDraftEnhancerPool")
                    .SetValue(data, null);
            }

            //handle champion data
            var champions = configuration.GetSection("champions").GetChildren().ToList();
            var championDatas =
                (List<ChampionData>)
                    AccessTools.Field(typeof(ClassData), "champions").GetValue(data) ?? [];

            if (overrideMode == OverrideMode.Replace && champions.Count > 0)
            {
                championDatas.Clear();
            }

            // TODO fix override with championData
            foreach (var champion in champions)
            {
                var championData = ScriptableObject.CreateInstance<ChampionData>();

                //string
                championData.championSelectedCue =
                    configuration.GetSection("selected_cue").ParseString() ?? "";

                //card data
                var championCardData = champion.GetSection("card_data").ParseReference();
                if (
                    championCardData != null
                    && cardDataRegister.TryLookupName(
                        championCardData.ToId(key, TemplateConstants.Card),
                        out var championCard,
                        out var _
                    )
                )
                {
                    championData.championCardData = championCard;
                }

                var starterCardData = champion.GetSection("starter_card").ParseReference();
                if (
                    starterCardData != null
                    && cardDataRegister.TryLookupName(
                        starterCardData.ToId(key, TemplateConstants.Card),
                        out var starterCard,
                        out var _
                    )
                )
                {
                    championData.starterCardData = starterCard;
                }

                //sprites
                var championIcon = champion.GetSection("icon").ParseReference();
                if (
                    championIcon != null
                    && spriteRegister.TryLookupName(
                        championIcon.ToId(key, TemplateConstants.Sprite),
                        out var icon1,
                        out var _
                    )
                )
                {
                    championData.championIcon = icon1;
                }

                var championLockedIcon = champion.GetSection("locked_icon").ParseReference();
                if (
                    championLockedIcon != null
                    && spriteRegister.TryLookupName(
                        championLockedIcon.ToId(key, TemplateConstants.Sprite),
                        out var icon2,
                        out var _
                    )
                )
                {
                    championData.championLockedIcon = icon2;
                }

                var championPortrait = champion.GetSection("portrait").ParseReference();
                if (
                    championPortrait != null
                    && spriteRegister.TryLookupName(
                        championPortrait.ToId(key, TemplateConstants.Sprite),
                        out var icon3,
                        out var _
                    )
                )
                {
                    championData.championPortrait = icon3;
                }

                //upgrade upgrade tree
                var upgradeTreeData = ScriptableObject.CreateInstance<CardUpgradeTreeData>();
                if (championData.championCardData != null)
                {
                    var championSpawn = championData.championCardData.GetSpawnCharacterData();
                    if (championSpawn != null)
                    {
                        // TODO this may not be necessary. Unused field.
                        AccessTools
                            .Field(typeof(CardUpgradeTreeData), "champion")
                            .SetValue(upgradeTreeData, championSpawn);
                    }
                }
                var upgradeTrees = new List<CardUpgradeTreeData.UpgradeTree>();
                AccessTools
                    .Field(typeof(CardUpgradeTreeData), "upgradeTrees")
                    .SetValue(upgradeTreeData, upgradeTrees);
                var upgradeTreeConfig = champion.GetSection("upgrade_tree").GetChildren();
                foreach (var config in upgradeTreeConfig)
                {
                    var upgradeTree = new CardUpgradeTreeData.UpgradeTree();
                    var upgrades = new List<CardUpgradeData>();
                    AccessTools
                        .Field(typeof(CardUpgradeTreeData.UpgradeTree), "cardUpgrades")
                        .SetValue(upgradeTree, upgrades);
                    var upgradeReferences = config
                        .GetChildren()
                        .Select(x => x.ParseReference())
                        .Where(x => x != null)
                        .Cast<ReferencedObject>();
                    foreach (var upgradeReference in upgradeReferences)
                    {
                        if (upgradeDataRegister.TryLookupName(
                                upgradeReference.ToId(key, TemplateConstants.Upgrade),
                                out var upgradeObj,
                                out var _
                            )
                        )
                        {
                            upgrades.Add(upgradeObj);
                        }
                    }
                    upgradeTrees.Add(upgradeTree);
                }

                AccessTools
                    .Field(typeof(ChampionData), "upgradeTree")
                    .SetValue(championData, upgradeTreeData);

                championDatas.Add(championData);
            }

            AccessTools.Field(typeof(ClassData), "champions").SetValue(data, championDatas);
        }

    }
}
