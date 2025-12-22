using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using ShinyShoe.Audio;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Base.Sound;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static ShinyShoe.Audio.CoreSoundEffectData;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Card
{
    public class CardDataFinalizer : IDataFinalizer
    {
        private readonly IModLogger<CardDataFinalizer> logger;
        private readonly ICache<IDefinition<CardData>> cache;
        private readonly IRegister<ClassData> classRegister;
        private readonly IRegister<CardData> cardRegister;
        private readonly IRegister<CardPool> cardPoolRegister;
        private readonly IRegister<CardTraitData> traitRegister;
        private readonly IRegister<CardEffectData> effectRegister;
        private readonly IRegister<AssetReferenceGameObject> assetReferenceRegister;
        private readonly IRegister<GameObject> gameObjectRegister;
        private readonly IRegister<CardUpgradeData> upgradeRegister;
        private readonly IRegister<CardTriggerEffectData> triggerEffectRegister;
        private readonly IRegister<CharacterTriggerData> triggerDataRegister;
        private readonly IRegister<VfxAtLoc> vfxRegister;
        private readonly IRegister<SoundCueDefinition> soundCueRegister;
        private readonly FallbackDataProvider fallbackDataProvider;

        private readonly FieldInfo CardPoolCardDataListField = AccessTools.Field(typeof(CardPool), "cardDataList");

        public CardDataFinalizer(
            IModLogger<CardDataFinalizer> logger,
            ICache<IDefinition<CardData>> cache,
            IRegister<ClassData> classRegister,
            IRegister<CardData> cardRegister,
            IRegister<CardPool> cardPoolRegister,
            IRegister<CardTraitData> traitRegister,
            IRegister<CardEffectData> effectRegister,
            IRegister<AssetReferenceGameObject> assetReferenceRegister,
            IRegister<GameObject> gameObjectRegister,
            IRegister<CardUpgradeData> upgradeRegister,
            IRegister<CardTriggerEffectData> triggerEffectRegister,
            IRegister<CharacterTriggerData> triggerDataRegister,
            IRegister<SoundCueDefinition> soundCueRegister,
            IRegister<VfxAtLoc> vfxRegister,
            FallbackDataProvider fallbackDataProvider
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.classRegister = classRegister;
            this.cardRegister = cardRegister;
            this.cardPoolRegister = cardPoolRegister;
            this.traitRegister = traitRegister;
            this.effectRegister = effectRegister;
            this.assetReferenceRegister = assetReferenceRegister;
            this.gameObjectRegister = gameObjectRegister;
            this.upgradeRegister = upgradeRegister;
            this.triggerEffectRegister = triggerEffectRegister;
            this.triggerDataRegister = triggerDataRegister;
            this.vfxRegister = vfxRegister;
            this.soundCueRegister = soundCueRegister;
            this.fallbackDataProvider = fallbackDataProvider;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeCardData(definition);
            }
            cache.Clear();
        }

        /// <summary>
        /// Finalize Card Definitions
        /// Handles Data to avoid lookup looks for names and ids
        /// </summary>
        /// <param name="definition"></param>
        private void FinalizeCardData(IDefinition<CardData> definition)
        {
            var configuration = definition.Configuration;
            var data = definition.Data;
            var key = definition.Key;
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();
            var newlyCreatedContent = overrideMode.IsNewContent();

            logger.Log(LogLevel.Info, $"Finalizing Card {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            //handle linked class
            var classfield = configuration.GetSection("class").ParseReference();
            if (
                classfield != null
                && classRegister.TryLookupName(
                    classfield.ToId(key, TemplateConstants.Class),
                    out var lookup,
                    out var _,
                    classfield.context
                )
            )
            {
                AccessTools.Field(typeof(CardData), "linkedClass").SetValue(data, lookup);
            }

            //handle discovery cards
            var SharedDiscoveryCards = data.GetSharedDiscoveryCards();
            var SharedDiscoveryCardsConfig = configuration.GetSection("shared_discovery_cards");
            if (overrideMode == OverrideMode.Replace && SharedDiscoveryCardsConfig.Exists())
            {
                SharedDiscoveryCards.Clear();
            }
            var SharedDiscoveryCardReferences = SharedDiscoveryCardsConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var cardReference in SharedDiscoveryCardReferences)
            {
                if (
                    cardRegister.TryLookupName(
                        cardReference.ToId(key, TemplateConstants.Card),
                        out var card,
                        out var _,
                        cardReference.context
                    )
                )
                {
                    SharedDiscoveryCards.Add(card);
                }
            }
            AccessTools
                .Field(typeof(CardData), "sharedDiscoveryCards")
                .SetValue(data, SharedDiscoveryCards);

            //handle mastery cards
            var SharedMasteryCards = data.GetSharedMasteryCards();
            var SharedMasteryCardConfig = configuration
                .GetSection("shared_mastery_cards");
            if (overrideMode == OverrideMode.Replace && SharedMasteryCardConfig.Exists())
            {
                SharedMasteryCards.Clear();
            }
            var sharedMasteryCardReferences = SharedMasteryCardConfig.GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var cardReference in sharedMasteryCardReferences)
            {
                if (
                    cardRegister.TryLookupName(
                        cardReference.ToId(key, TemplateConstants.Card),
                        out var card,
                        out var _,
                        cardReference.context
                    )
                )
                {
                    SharedMasteryCards.Add(card);
                }
            }
            AccessTools
                .Field(typeof(CardData), "sharedMasteryCards")
                .SetValue(data, SharedMasteryCards);

            //handle linked mastery card
            var MasteryCardConfig = configuration.GetDeprecatedSection("mastery_card", "linked_mastery_card");
            var MasteryCardRef = MasteryCardConfig.ParseReference();
            if (MasteryCardRef != null)
            {
                cardRegister.TryLookupName(MasteryCardRef.ToId(key, TemplateConstants.Card), out var MasteryCard, out var _, MasteryCardRef.context);
                AccessTools
                    .Field(typeof(CardData), "linkedMasteryCard")
                    .SetValue(data, MasteryCard);
            }
            if (overrideMode == OverrideMode.Replace && MasteryCardConfig.Exists() && MasteryCardRef == null)
            {
                AccessTools
                    .Field(typeof(CardData), "linkedMasteryCard")
                    .SetValue(data, null);
            }

            //handle art (required field so don't allow override with null)
            var cardArtReference = configuration.GetDeprecatedSection("card_art_reference", "card_art").ParseReference();
            if (cardArtReference != null)
            {
                if (
                    assetReferenceRegister.TryLookupId(
                        cardArtReference.ToId(key, TemplateConstants.GameObject),
                        out var gameObject,
                        out var _,
                        cardArtReference.context
                    )
                )
                {
                    AccessTools
                        .Field(typeof(CardData), "cardArtPrefabVariantRef")
                        .SetValue(data, gameObject);
                }
            }
            else if (data.IsUnitAbility())
            {
                AccessTools.Field(typeof(CardData), "cardArtPrefabVariantRef").SetValue(data, new AssetReferenceGameObject());
            }
            else if (overrideMode.IsNewContent())
            {
                logger.Log(LogLevel.Warning, $"Card {key} {definition.Id} is missing card_art. This is required for non ability cards.");
            }

            var soundEffects = configuration.GetSection("sound_effects").GetChildren().Select(x => x.ParseReference()).Where(x => x != null).Cast<ReferencedObject>();
            if (soundEffects.Any() && cardArtReference != null)
            {
                if (gameObjectRegister.TryLookupId(cardArtReference.ToId(key, TemplateConstants.GameObject), out var gameObject, out var _, cardArtReference.context))
                {
                    var holder = gameObject.AddComponent<CoreSoundEffectHolder>();
                    holder.SoundEffectData = ScriptableObject.CreateInstance<CoreSoundEffectData>();
                    List<SoundCueDefinition> sounds = [];
                    foreach (var soundReference in soundEffects)
                    {
                        if (soundCueRegister.TryLookupName(soundReference.ToId(key, TemplateConstants.SoundCueDefinition), out var sound, out var _2, soundReference.context))
                        {
                            sounds.Add(sound);
                        }
                    }
                    holder.SoundEffectData.Sounds = sounds.ToArray();
                }
            }

            //handle traits
            var cardTraitDatas = data.GetTraits();
            var cardTraitConfig = configuration.GetSection("traits");
            if (overrideMode == OverrideMode.Replace && cardTraitConfig.Exists())
            {
                cardTraitDatas.Clear();
            }
            var cardTraitReferences = cardTraitConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var traitReference in cardTraitReferences)
            {
                var id = traitReference.ToId(key, TemplateConstants.Trait);
                if (traitRegister.TryLookupId(id, out var trait, out var _, traitReference.context))
                {
                    cardTraitDatas.Add(trait);
                }
            }
            AccessTools.Field(typeof(CardData), "traits").SetValue(data, cardTraitDatas);

            var cardEffectDatas = data.GetEffects();
            var cardEffectDatasConfig = configuration.GetSection("effects");
            if (overrideMode == OverrideMode.Replace && cardEffectDatasConfig.Exists())
            {
                cardEffectDatas.Clear();
            }
            var cardEffectDatasReferences = cardEffectDatasConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var effectReference in cardEffectDatasReferences)
            {
                if (effectRegister.TryLookupId(effectReference.ToId(key, TemplateConstants.Effect), out var effect, out var _, effectReference.context))
                {
                    cardEffectDatas.Add(effect);
                }
            }
            AccessTools.Field(typeof(CardData), "effects").SetValue(data, cardEffectDatas);

            var cardTriggers = data.GetCardTriggers();
            var cardTriggerEffectDataConfig = configuration.GetSection("triggers");
            if (overrideMode == OverrideMode.Replace && cardTriggerEffectDataConfig.Exists())
            {
                cardTriggers.Clear();
            }
            var cardTriggerReferences = cardTriggerEffectDataConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var triggerReference in cardTriggerReferences)
            {
                var id = triggerReference.ToId(key, TemplateConstants.CardTrigger);
                if (triggerEffectRegister.TryLookupId(id, out var trigger, out var _, triggerReference.context))
                {
                    cardTriggers.Add(trigger);
                }
            }
            AccessTools.Field(typeof(CardData), "triggers").SetValue(data, cardTriggers);

            List<CardUpgradeData> initialUpgrades = [];
            var initialUpgradesRO = data.GetUpgradeData();
            var initialUpgradesConfig = configuration.GetSection("initial_upgrades");
            if (!(overrideMode == OverrideMode.Replace && initialUpgradesConfig.Exists()))
            {
                initialUpgrades.AddRange(initialUpgradesRO);
            }
            var initialUpgradeReferences = initialUpgradesConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var upgradeReference in initialUpgradeReferences)
            {
                var id = upgradeReference.ToId(key, TemplateConstants.Upgrade);
                if (upgradeRegister.TryLookupName(id, out var upgrade, out var _, upgradeReference.context))
                {
                    initialUpgrades.Add(upgrade);
                }
            }
            AccessTools
                    .Field(typeof(CardData), "startingUpgrades")
                    .SetValue(data, initialUpgrades);

            var effectTriggers = data.GetEffectTriggers();
            var effectTriggerConfig = configuration.GetSection("effect_triggers");
            if (overrideMode == OverrideMode.Replace && effectTriggerConfig.Exists())
            {
                effectTriggers.Clear();
            }
            var effectTriggerReferences = effectTriggerConfig
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var characterTriggerReference in effectTriggerReferences)
            {
                var id = characterTriggerReference.ToId(key, TemplateConstants.CharacterTrigger);
                if (triggerDataRegister.TryLookupId(id, out var trigger, out var _, characterTriggerReference.context))
                {
                    effectTriggers.Add(trigger);
                }
            }
            AccessTools.Field(typeof(CardData), "effectTriggers").SetValue(data, effectTriggers);

            // Do not allow the vfx to be set to null. As they are soft required. If not set then they are set to a Default VFX. Setting to null will crash the game.
            var offCooldownVFXReference = configuration.GetDeprecatedSection("vfx", "off_cooldown_vfx").ParseReference();
            if (newlyCreatedContent || offCooldownVFXReference != null)
            {
                var id = offCooldownVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "";
                vfxRegister.TryLookupId(id, out var offCooldownVfx, out var _, offCooldownVFXReference?.context);
                AccessTools.Field(typeof(CardData), "offCooldownVFX").SetValue(data, offCooldownVfx);
            }

            var specialEdgeVFXReference = configuration.GetSection("special_edge_vfx").ParseReference();
            if (newlyCreatedContent || specialEdgeVFXReference != null)
            {
                var id = specialEdgeVFXReference?.ToId(key, TemplateConstants.Vfx) ?? "";
                vfxRegister.TryLookupId(id, out var specialEdgeVfx, out var _, offCooldownVFXReference?.context);
                AccessTools.Field(typeof(CardData), "specialEdgeVFX").SetValue(data, specialEdgeVfx);
            }

            var poolReferences = configuration.GetSection("pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var poolReference in poolReferences)
            {
                var id = poolReference.ToId(key, TemplateConstants.CardPool);
                if (cardPoolRegister.TryLookupId(id, out var pool, out var _, poolReference.context))
                {
                    var cardDataList = CardPoolCardDataListField.GetValue(pool) as ReorderableArray<CardData>;
                    cardDataList?.Add(data);
                }
            }

            AccessTools
                .Field(typeof(CardData), "fallbackData")
                .SetValue(data, fallbackDataProvider.FallbackData);
        }
    }
}
