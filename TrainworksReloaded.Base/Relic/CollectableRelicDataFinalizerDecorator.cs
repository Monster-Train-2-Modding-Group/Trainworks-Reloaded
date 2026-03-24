using HarmonyLib;
using Malee;
using Microsoft.Extensions.Configuration;
using Spine;
using System;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Relic
{
    public class CollectableRelicDataFinalizerDecorator : IDataFinalizer
    {
        private readonly IModLogger<CollectableRelicDataFinalizerDecorator> logger;
        private readonly ICache<IDefinition<RelicData>> cache;
        private readonly IRegister<ClassData> classRegister;
        private readonly IRegister<RelicPool> relicPoolRegister;
        private readonly IRegister<CardPool> cardPoolRegister;
        private readonly IRegister<SubtypeData> subtypeRegister;
        private readonly IRegister<LocalizationTerm> localizationRegister;
        private readonly PluginAtlas atlas;
        private readonly IDataFinalizer decoratee;

        private readonly FieldInfo RelicPoolRelicDataListField = AccessTools.Field(typeof(RelicPool), "relicDataList");
        private readonly FieldInfo LinkedClassField = AccessTools.Field(typeof(CollectableRelicData), "linkedClass");

        public CollectableRelicDataFinalizerDecorator(
            IModLogger<CollectableRelicDataFinalizerDecorator> logger,
            ICache<IDefinition<RelicData>> cache,
            PluginAtlas atlas,
            IRegister<ClassData> classRegister,
            IRegister<RelicPool> relicPoolRegister,
            IRegister<SubtypeData> subtypeRegister,
            IRegister<CardPool> cardPoolRegister,
            IRegister<LocalizationTerm> localizationRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.atlas = atlas;
            this.classRegister = classRegister;
            this.relicPoolRegister = relicPoolRegister;
            this.subtypeRegister = subtypeRegister;
            this.cardPoolRegister = cardPoolRegister;
            this.localizationRegister = localizationRegister;
            this.decoratee = decoratee;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeRelicData((definition as RelicDataDefinition)!);
            }
            decoratee.FinalizeData();
            cache.Clear();
        }

        private void FinalizeRelicData(RelicDataDefinition definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var overrideMode = definition.Override;
            var key = definition.Key;
            var relicId = definition.Id.ToId(key, TemplateConstants.RelicData);

            if (data is not CollectableRelicData relic)
                return;

            if (definition.CopyData is not CollectableRelicData copyData)
                copyData = relic;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("collectable").Exists())
                .Select(xs => xs.GetSection("collectable"))
                .FirstOrDefault();
            if (configuration == null)
                return;

            logger.Log(LogLevel.Info, $"Finalizing Collectable Relic Data {definition.Key} {definition.Id} path: {configuration.GetPath()}...");

            // Handle linked class
            var linkedClassReference = configuration.GetSection("class").ParseReference();
            var linkedClass = copyData.GetLinkedClass();
            if (linkedClassReference != null && classRegister.TryLookupName(linkedClassReference.ToId(key, TemplateConstants.Class), out var lookup, out var _, linkedClassReference.context))
            {
                linkedClass = lookup;
            }
            LinkedClassField.SetValue(relic, linkedClass);

            var poolReferences = configuration.GetSection("pools")
                .GetChildren()
                .Select(x => x.ParseReference())
                .Where(x => x != null)
                .Cast<ReferencedObject>();
            foreach (var poolReference in poolReferences)
            {
                var id = poolReference.ToId(key, TemplateConstants.RelicPool);
                if (relicPoolRegister.TryLookupId(id, out var pool, out var _, poolReference.context))
                {
                    var relicDataList = RelicPoolRelicDataListField.GetValue(pool) as ReorderableArray<CollectableRelicData>;
                    relicDataList?.Add(relic);
                    logger.Log(LogLevel.Debug, $"Added relic {definition.Id.ToId(key, TemplateConstants.RelicData)} to pool: {pool}");
                }
            }

            var descriptionModifiers = configuration.GetSection("description_modifiers");
            ParseDescriptionModifiers(descriptionModifiers, key, relic, copyData, overrideMode);
        }

        private void ParseDescriptionModifiers(IConfigurationSection configuration, string key, CollectableRelicData data, CollectableRelicData copyData, OverrideMode overrideMode)
        {
            var descriptionModifiers = copyData.GetDescriptionModifiers() ?? [];
            if (copyData != data)
                descriptionModifiers = [.. descriptionModifiers];
            if (overrideMode == OverrideMode.Replace && configuration.Exists())
            {
                descriptionModifiers.Clear();
            }
            int i = 0;
            foreach (var item in configuration.GetChildren())
            {
                descriptionModifiers.Add(ParseDescriptionModifier(item, key, data.name, i));
                i++;
            }
            AccessTools.Field(typeof(CollectableRelicData), "descriptionModifiers").SetValue(data, descriptionModifiers);
        }

        private SerializableDescriptionModifier? ParseDescriptionModifier(IConfigurationSection config, string key, string relicId, int index)
        {
            if (!config.Exists()) return null;

            var data = new SerializableDescriptionModifier();

            var textFormatKey = $"CollectableRelicData_textFormatKey{index}-{relicId}";

            var effectStateName = config.GetSection("name").Value;
            if (effectStateName == null)
                return null;

            var modReference = config.GetSection("mod_reference").Value ?? key;
            var assembly = atlas.PluginDefinitions.GetValueOrDefault(modReference)?.Assembly;
            if (
                !effectStateName.GetFullyQualifiedName<DescriptionModifier>(
                    assembly,
                    out string? fullyQualifiedName
                )
            )
            {
                logger.Log(LogLevel.Error, $"Failed to load description modifier class {effectStateName} in {relicId} mod {modReference}, Make sure the class exists in {modReference} and that the class inherits from DescriptionModifier.");
                return null;
            }
            AccessTools.Field(typeof(SerializableDescriptionModifier), "modifierClassName").SetValue(data, fullyQualifiedName);

            var paramInt = config.GetSection("param_int").ParseInt() ?? 0;
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramInt").SetValue(data, paramInt);

            var paramFloat = config.GetSection("param_float").ParseFloat() ?? 0;
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramFloat").SetValue(data, paramFloat);

            var paramBool = config.GetSection("param_bool").ParseBool() ?? false;
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramBool").SetValue(data, paramBool);

            var paramBool2 = config.GetSection("param_bool_2").ParseBool() ?? false;
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramBool2").SetValue(data, paramBool2);

            var cardRarityType = config.GetSection("param_card_rarity_type").ParseRarity() ?? CollectableRarity.Common;
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramCardRarityType").SetValue(data, cardRarityType);

            var cardType = config.GetSection("param_card_type").ParseCardType() ?? CardType.Invalid;
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramCardType").SetValue(data, cardType);

            var paramString = config.GetSection("param_string").ParseString() ?? "";
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramString").SetValue(data, paramString);

            var cardPoolReference = config.GetSection("param_card_pool").ParseReference();
            if (cardPoolReference != null && cardPoolRegister.TryLookupId(cardPoolReference.ToId(key, TemplateConstants.CardPool), out var cardPool, out var _, cardPoolReference.context))
            {
                AccessTools.Field(typeof(SerializableDescriptionModifier), "paramCardPool").SetValue(data, cardPool);
            }

            var characterSubtype = "SubtypesData_None";
            var characterSubtypeReference = config.GetSection("param_subtype").ParseReference();
            if (characterSubtypeReference != null)
            {
                if (subtypeRegister.TryLookupId(
                    characterSubtypeReference.ToId(key, TemplateConstants.Subtype),
                    out var lookup,
                    out var _, characterSubtypeReference.context))
                {
                    characterSubtype = lookup.Key;
                }
            }
            AccessTools.Field(typeof(SerializableDescriptionModifier), "paramCharacterSubtype").SetValue(data, characterSubtype);

            var formatTerm = config.GetSection("text_formats").ParseLocalizationTerm();
            if (formatTerm != null)
            {
                AccessTools.Field(typeof(SerializableDescriptionModifier), "textFormatKey").SetValue(data, textFormatKey);
                formatTerm.Key = textFormatKey;
                localizationRegister.Register(textFormatKey, formatTerm);
            }

            return data;
        }
    }
}