using SimpleInjector;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using static CardStatistics;
using static ShinyShoe.Audio.CoreSoundEffectData;
using static StatusEffectData;
using static TooltipDesigner;

namespace TrainworksReloaded.Base
{
    /// <summary>
    /// Helper class to query for GameData.
    /// </summary>
    public class GameDataManager
    {
        private readonly Container container;
        private readonly string defaultPluginGuid;
        internal GameDataManager(Container container, string pluginGuid)
        { 
            this.container = container;
            this.defaultPluginGuid = pluginGuid;
        }

        public CardData GetCard(string id, string? pluginGuid = null) =>
                GetFromRegister<CardData>(TemplateConstants.Card, id, pluginGuid)!;

        public CardUpgradeData GetUpgrade(string id, string? pluginGuid = null) =>
            GetFromRegister<CardUpgradeData>(TemplateConstants.Upgrade, id, pluginGuid)!;

        public CardUpgradeData GetCardUpgrade(string id, string? pluginGuid = null) =>
            GetUpgrade(id, pluginGuid)!;

        public CardUpgradeMaskData GetFilter(string id, string? pluginGuid = null) =>
            GetCardUpgradeMask(id, pluginGuid);

        public CardUpgradeMaskData GetCardUpgradeMask(string id, string? pluginGuid = null) =>
            GetFromRegister<CardUpgradeMaskData>(TemplateConstants.UpgradeMask, id, pluginGuid)!;

        public RewardData GetReward(string id, string? pluginGuid = null) =>
            GetReward<RewardData>(id, pluginGuid);

        public T GetReward<T>(string id, string? pluginGuid = null) where T : RewardData =>
            (GetFromRegister<RewardData>(TemplateConstants.RewardData, id, pluginGuid) as T)!;

        public AudioClip GetAudioClip(string id, string? pluginGuid = null) =>
                GetFromRegister<AudioClip>(TemplateConstants.AudioClip, id, pluginGuid)!;

        public SoundCueDefinition GetSoundCueDefinition(string id, string? pluginGuid = null) =>
            GetFromRegister<SoundCueDefinition>(TemplateConstants.SoundCueDefinition, id, pluginGuid)!;

        public AssetBundle GetAssetBundle(string id, string? pluginGuid = null) =>
            GetFromRegister<AssetBundle>(TemplateConstants.AssetBundle, id, pluginGuid)!;

        public Sprite GetSprite(string id, string? pluginGuid = null) =>
            GetFromRegister<Sprite>(TemplateConstants.Sprite, id, pluginGuid)!;

        public Texture2D GetTexture(string id, string? pluginGuid = null) =>
            GetFromRegister<Texture2D>(TemplateConstants.Sprite, id, pluginGuid)!;

        public SkeletonDataAsset GetSkeletonData(string id, string? pluginGuid = null) =>
            GetFromRegister<SkeletonDataAsset>(TemplateConstants.SkeletonData, id, pluginGuid)!;

        public CharacterTriggerData.Trigger GetCharacterTriggerEnum(string id, string? pluginGuid = null) =>
            GetFromRegister<CharacterTriggerData.Trigger>(TemplateConstants.CharacterTriggerEnum, id, pluginGuid)!;

        public CardTriggerType GetCardTriggerEnum(string id, string? pluginGuid = null) =>
            GetFromRegister<CardTriggerType>(TemplateConstants.CardTriggerEnum, id, pluginGuid)!;

        public TargetMode GetTargetModeEnum(string id, string? pluginGuid = null) =>
            GetFromRegister<TargetMode>(TemplateConstants.TargetModeEnum, id, pluginGuid)!;

        public StatusEffectData.TriggerStage GetStatusEffectTriggerStageEnum(string id, string? pluginGuid = null) =>
            GetFromRegister<StatusEffectData.TriggerStage>(TemplateConstants.StatusEffectTriggerStageEnum, id, pluginGuid)!;

        public TrackedValueType GetTrackedValueTypeEnum(string id, string? pluginGuid = null) =>
            GetFromRegister<TrackedValueType>(TemplateConstants.TrackedValueTypeEnum, id, pluginGuid)!;

        public TooltipDesignType GetTooltipDesignTypeEnum(string id, string? pluginGuid = null) =>
            GetFromRegister<TooltipDesignType>(TemplateConstants.TooltipDesignTypeEnum, id, pluginGuid)!;

        public ClassCardStyle GetClassCardStyleEnum(string id, string? pluginGuid = null) =>
            GetFromRegister<ClassCardStyle>(TemplateConstants.ClassCardStyle, id, pluginGuid)!;

        public SubtypeData GetSubtype(string id, string? pluginGuid = null) =>
            GetFromRegister<SubtypeData>(TemplateConstants.Subtype, id, pluginGuid)!;

        public StatusEffectData GetStatusEffect(string id, string? pluginGuid = null) =>
            GetFromRegister<StatusEffectData>(TemplateConstants.StatusEffect, id, pluginGuid)!;

        public AdditionalTooltipData GetAdditionalTooltip(string id, string? pluginGuid = null) =>
            GetFromRegister<AdditionalTooltipData>(TemplateConstants.AdditionalTooltip, id, pluginGuid)!;

        public CardPool GetCardPool(string id, string? pluginGuid = null) =>
            GetFromRegister<CardPool>(TemplateConstants.CardPool, id, pluginGuid)!;

        public EnhancerPool GetEnhancerPool(string id, string? pluginGuid = null) =>
            GetFromRegister<EnhancerPool>(TemplateConstants.EnhancerPool, id, pluginGuid)!;

        public SoulPool GetSoulPool(string id, string? pluginGuid = null) =>
            GetFromRegister<SoulPool>(TemplateConstants.SoulPool, id, pluginGuid)!;

        public RelicPool GetRelicPool(string id, string? pluginGuid = null) =>
            GetFromRegister<RelicPool>(TemplateConstants.RelicPool, id, pluginGuid)!;

        public CharacterData GetCharacter(string id, string? pluginGuid = null) =>
            GetFromRegister<CharacterData>(TemplateConstants.Character, id, pluginGuid)!;

        public PyreHeartData GetPyreHeart(string id, string? pluginGuid = null) =>
            GetFromRegister<PyreHeartData>(TemplateConstants.PyreHeart, id, pluginGuid)!;

        public CardTriggerEffectData GetCardTrigger(string id, string? pluginGuid = null) =>
            GetFromRegister<CardTriggerEffectData>(TemplateConstants.CardTrigger, id, pluginGuid)!;

        public CharacterTriggerData GetCharacterTrigger(string id, string? pluginGuid = null) =>
            GetFromRegister<CharacterTriggerData>(TemplateConstants.CharacterTrigger, id, pluginGuid)!;

        public CharacterChatterData GetChatter(string id, string? pluginGuid = null) =>
            GetFromRegister<CharacterChatterData>(TemplateConstants.Chatter, id, pluginGuid)!;

        public ClassData GetClass(string id, string? pluginGuid = null) =>
            GetFromRegister<ClassData>(TemplateConstants.Class, id, pluginGuid)!;

        public ClassData GetClan(string id, string? pluginGuid = null) => GetClass(id, pluginGuid);

        public CardTraitData GetCardTrait(string id, string? pluginGuid = null) =>
            GetTrait(id, pluginGuid)!;

        public CardTraitData GetTrait(string id, string? pluginGuid = null) =>
            GetFromRegister<CardTraitData>(TemplateConstants.Trait, id, pluginGuid)!;

        public MapNodeData GetMapNode(string id, string? pluginGuid = null) =>
            GetFromRegister<MapNodeData>(TemplateConstants.MapNode, id, pluginGuid)!;

        public RoomModifierData GetRoomModifier(string id, string? pluginGuid = null) =>
            GetFromRegister<RoomModifierData>(TemplateConstants.RoomModifier, id, pluginGuid)!;

        public SpChallengeData GetSpChallenge(string id, string? pluginGuid = null) =>
            GetFromRegister<SpChallengeData>(TemplateConstants.Challenge, id, pluginGuid)!;

        public ScenarioData GetScenario(string id, string? pluginGuid = null) =>
            GetFromRegister<ScenarioData>(TemplateConstants.Scenario, id, pluginGuid)!;

        public TrialData GetTrial(string id, string? pluginGuid = null) =>
            GetFromRegister<TrialData>(TemplateConstants.Trial, id, pluginGuid)!;

        public RelicData GetRelic(string id, string? pluginGuid = null) =>
            GetRelic<RelicData>(id, pluginGuid)!;

        public T GetRelic<T>(string id, string? pluginGuid = null)  where T : RelicData =>
            (GetFromRegister<RelicData>(TemplateConstants.RelicData, id, pluginGuid) as T)!;

        public VfxAtLoc GetVfx(string id, string? pluginGuid = null) =>
            GetFromRegister<VfxAtLoc>(TemplateConstants.Vfx, id, pluginGuid)!;

        public CardEffectData GetCardEffect(string id, string? pluginGuid = null) =>
            GetFromRegister<CardEffectData>(TemplateConstants.Effect, id, pluginGuid)!;

        public RelicEffectData GetRelicEffect(string id, string? pluginGuid = null) =>
            GetFromRegister<RelicEffectData>(TemplateConstants.RelicEffectData, id, pluginGuid)!;

        public RelicEffectCondition GetRelicEffectCondition(string id, string? pluginGuid = null) =>
            GetFromRegister<RelicEffectCondition>(TemplateConstants.RelicEffectCondition, id, pluginGuid)!;

        public StoryEventData GetStoryEvent(string id, string? pluginGuid = null) =>
            GetFromRegister<StoryEventData>(TemplateConstants.StoryEvent, id, pluginGuid)!;

        public SaveManager GetSaveManager()
        {
            // Should always be available, generally.
            return GetProvider<SaveManager>()!;
        }

        private T? GetFromRegister<T>(string templateConstant, string id, string? pluginGuid = null)
        {
            var register = container.GetInstance<IRegister<T>>();
            var resolvedGuid = pluginGuid ?? defaultPluginGuid;
            var fullId = resolvedGuid.GetId(templateConstant, id);
            return register.GetValueOrDefault(fullId);
        }

        public T? GetProvider<T>() where T : IProvider
        {
            var gameDataClient = container.GetInstance<GameDataClient>();
            gameDataClient.TryGetProvider<T>(out var provider);
            return provider;
        }
    }
}
