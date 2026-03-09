using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using ShinyShoe.Logging;
using SimpleInjector;
using Spine;
using Spine.Unity;
using TrainworksReloaded.Base;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.CardUpgrade;
using TrainworksReloaded.Base.Challenges;
using TrainworksReloaded.Base.Character;
using TrainworksReloaded.Base.Class;
using TrainworksReloaded.Base.Effect;
using TrainworksReloaded.Base.Enums;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Map;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Base.Pyre;
using TrainworksReloaded.Base.Relic;
using TrainworksReloaded.Base.Reward;
using TrainworksReloaded.Base.Room;
using TrainworksReloaded.Base.Scenarios;
using TrainworksReloaded.Base.Sound;
using TrainworksReloaded.Base.StatusEffects;
using TrainworksReloaded.Base.Subtype;
using TrainworksReloaded.Base.Tooltips;
using TrainworksReloaded.Base.Trait;
using TrainworksReloaded.Base.Trials;
using TrainworksReloaded.Base.Trigger;
using TrainworksReloaded.Core;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static ShinyShoe.Audio.CoreSoundEffectData;

namespace TrainworksReloaded.Plugin
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = new("TrainworksReloaded");
        internal static Lazy<Container> Container = new(() => Railend.GetContainer());
        internal static Lazy<GameDataClient> Client = new(() => new GameDataClient());

        public void Awake()
        {
            //Pregame Actions
            var configToolsCSV = Config.Bind(
                "Tools",
                "Generate CSVs",
                false,
                "Enable to Generate the Games' CSV Files on Launch"
            );

            var configIncludeGameLogs = Config.Bind(
                "Logs",
                "Include Game Logs",
                false,
                "Enable Game Logs to BepInEx Console"
            );

            if (configToolsCSV.Value)
            {
                var basePath = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                LocalizationManager.InitializeIfNeeded();
                for (int i = 0; i < LocalizationManager.Sources.Count; i++)
                {
                    var source = LocalizationManager.Sources[i];
                    foreach (var category in source.GetCategories())
                    {
                        var str = source.Export_CSV(category);
                        var file_name = $"{i}_{category}.csv";
                        File.WriteAllText(Path.Combine(basePath, file_name), str);
                    }
                }
            }

            // Setup Game Client
            var client = Client.Value;
            DepInjector.AddClient(client);

            // Plugin startup logic
            Logger = base.Logger;
            if (configIncludeGameLogs.Value)
            {
                Log.AddProvider(new ModLogger<Plugin>(Logger));
            }

            Railend.ConfigurePreAction(c =>
            {
                //Register for Logging
                c.RegisterInstance(Logger);

                //Register hooking into games dependency injection system
                c.RegisterInstance(client);

                c.RegisterSingleton<IGuidProvider, DeterministicGuidGenerator>();

                c.Register<Finalizer, Finalizer>();
                c.Collection.Register<IDataFinalizer>(
                    [
                        typeof(AdditionalTooltipFinalizer),
                        typeof(CardEffectFinalizer),
                        typeof(CardTraitDataFinalizer),
                        typeof(CardUpgradeFinalizer),
                        typeof(CardUpgradeMaskFinalizer),
                        typeof(ClassDataFinalizer),
                        typeof(CharacterTriggerFinalizer),
                        typeof(CardTriggerEffectFinalizer),
                        typeof(VfxFinalizer),
                        typeof(AtlasIconFinalizer),
                        typeof(RoomModifierFinalizer),
                        typeof(StatusEffectDataFinalizer),
                        typeof(ScenarioDataFinalizer),
                        typeof(TrialDataFinalizer),
                        typeof(MapNodeFinalizer),
                        typeof(RewardDataFinalizer),
                        typeof(CardPoolFinalizer),
                        typeof(EnhancerPoolFinalizer),
                        typeof(RelicPoolFinalizer),
                        typeof(SoulPoolFinalizer),
                        typeof(CharacterTriggerTypeFinalizer),
                        typeof(CardTriggerTypeFinalizer),
                        typeof(CharacterChatterFinalizer),
                        typeof(RelicDataFinalizer),
                        typeof(RelicEffectDataFinalizer),
                        typeof(RelicEffectConditionFinalizer),
                        typeof(GameObjectFinalizer),
                        typeof(ClassCardStyleFinalizer),
                        typeof(SoundCueFinalizer),
                        typeof(PyreHeartDataFinalizer),
                        typeof(ChallengeDataFinalizer),
                        // These two have to run last. Sounds are added to the GameObjects which isn't available until GameObject finalizers has ran..
                        typeof(CharacterDataFinalizer),
                        typeof(CardDataFinalizer),
                    ]
                );

                RegisterPipeline<Base.Localization.LanguageSource, LanguageSourceRegister, LanguageSourcePipeline>(c);
                RegisterPipeline<LocalizationTerm, CustomLocalizationTermRegistry, LocalizationTermPipeline>(c);
                RegisterPipeline<ReplacementStringData, ReplacementStringRegistry, ReplacementStringPipeline>(c);


                c.RegisterConditional(
                    typeof(ICache<>),
                    typeof(Cache<>),
                    Lifestyle.Singleton,
                    c => !c.Handled
                );
                c.RegisterConditional(
                    typeof(IModLogger<>),
                    typeof(ModLogger<>),
                    Lifestyle.Singleton,
                    c => !c.Handled
                );

                c.RegisterConditional(
                    typeof(IInstanceGenerator<>),
                    typeof(ScriptableObjectInstanceGenerator<>),
                    Lifestyle.Transient,
                    c =>
                        typeof(ScriptableObject).IsAssignableFrom(
                            c.ServiceType.GetGenericArguments()[0]
                        )
                );
                c.RegisterConditional(
                    typeof(IInstanceGenerator<>),
                    typeof(InstanceGenerator<>),
                    c => !c.Handled
                );

                c.RegisterDecorator(
                    typeof(IDataPipeline<,>),
                    typeof(CacheDataPipelineDecorator<,>)
                );

                //Register Assets
                c.Register<FallbackDataProvider, FallbackDataProvider>();
                c.RegisterSingleton<IRegister<GameObject>, GameObjectRegister>();
                c.RegisterSingleton<GameObjectRegister, GameObjectRegister>();
                c.RegisterSingleton<
                    IRegister<AssetReferenceGameObject>,
                    AssetReferenceGameObjectRegister
                >();
                c.RegisterSingleton<
                    AssetReferenceGameObjectRegister,
                    AssetReferenceGameObjectRegister
                >();
                c.RegisterInitializer<GameObjectRegister>(x =>
                {
                    Addressables.ResourceLocators.Add(x);
                    Addressables.ResourceManager.ResourceProviders.Add(x);
                });
                c.RegisterInitializer<IRegister<GameObject>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<GameObject>, GameObject>
                    >();
                    pipeline.Run(x);
                });
                c.Register<
                    IDataPipeline<IRegister<GameObject>, GameObject>,
                    GameObjectImportPipeline
                >(); //a data pipeline to run as soon as register is needed
                c.RegisterDecorator<
                    IDataPipeline<IRegister<GameObject>, GameObject>,
                    GameObjectCardArtDecorator
                >();
                c.RegisterDecorator<
                    IDataPipeline<IRegister<GameObject>, GameObject>,
                    GameObjectMapIconDecorator
                >();
                c.RegisterDecorator<
                    IDataPipeline<IRegister<GameObject>, GameObject>,
                    GameObjectBattleIconDecorator
                >();
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(GameObjectCharacterArtFinalizer),
                    xs => xs.ImplementationType == typeof(GameObjectFinalizer)
                );

                RegisterPipeline<AudioClip, AudioClipRegister, AudioClipPipeline>(c);

                //Register Sound Effects
                c.RegisterSingleton<GlobalSoundCueDelegator, GlobalSoundCueDelegator>();
                RegisterPipeline<SoundCueDefinition, SoundCueRegister, SoundCuePipeline>(c);

                RegisterPipeline<Sprite, SpriteRegister, SpritePipeline>(c);
                RegisterPipeline<Texture2D, TextureRegister, AtlasIconPipeline>(c);
                RegisterPipeline<SkeletonDataAsset, SkeletonDataRegister, SkeletonDataPipeline>(c);
                RegisterPipeline<CharacterTriggerData.Trigger, CharacterTriggerTypeRegister, CharacterTriggerTypePipeline>(c);
                RegisterPipeline<CardTriggerType, CardTriggerTypeRegister, CardTriggerTypePipeline>(c);
                RegisterPipeline<TargetMode, TargetModeRegister, TargetModePipeline>(c);
                RegisterPipeline<StatusEffectData.TriggerStage, StatusEffectTriggerStageRegister, StatusEffectTriggerStagePipeline>(c);
                RegisterPipeline<CardStatistics.TrackedValueType, TrackedValueTypeRegister, TrackedValueTypePipeline>(c);

                c.RegisterSingleton<ClassCardStyleDelegator, ClassCardStyleDelegator>();
                RegisterPipeline<ClassCardStyle, ClassCardStyleRegister, ClassCardStylePipeline>(c);

                RegisterPipeline<SubtypeData, SubtypeDataRegister, SubtypeDataPipeline>(c);
                RegisterPipeline<StatusEffectData, StatusEffectDataRegister, StatusEffectDataPipeline>(c);
                RegisterPipeline<AdditionalTooltipData, AdditionalTooltipRegister, AdditionalTooltipPipeline>(c);
                RegisterPipeline<CardData, CardDataRegister, CardDataPipeline>(c);
                RegisterPipeline<CardPool, CardPoolRegister, CardPoolPipeline>(c);
                RegisterPipeline<EnhancerPool, EnhancerPoolRegister, EnhancerPoolPipeline>(c);
                RegisterPipeline<SoulPool, SoulPoolRegister, SoulPoolPipeline>(c);
                RegisterPipeline<RelicPool, RelicPoolRegister, RelicPoolPipeline>(c);
                RegisterPipeline<CharacterData, CharacterDataRegister, CharacterDataPipeline>(c);
                RegisterPipeline<PyreHeartData, PyreHeartDataRegister, PyreHeartDataPipeline>(c);
                RegisterPipeline<CardTriggerEffectData, CardTriggerEffectRegister, CardTriggerEffectPipeline>(c);
                RegisterPipeline<CharacterTriggerData, CharacterTriggerRegister, CharacterTriggerPipeline>(c);
                RegisterPipeline<CharacterChatterData, CharacterChatterRegister, CharacterChatterPipeline>(c);

                c.RegisterSingleton<ClassAssetsDelegator, ClassAssetsDelegator>();
                RegisterPipeline<ClassData, ClassDataRegister, ClassDataPipeline>(c);
                RegisterPipeline<CardEffectData, CardEffectDataRegister, CardEffectDataPipeline>(c);

                RegisterPipeline<MapNodeData, MapNodeRegister, MapNodePipeline>(c);
                c.Collection.Register<IFactory<MapNodeData>>(
                    [typeof(RewardNodeDataFactory)],
                    Lifestyle.Singleton
                );
                c.RegisterDecorator<
                    IDataPipeline<IRegister<MapNodeData>, MapNodeData>,
                    RewardNodeDataPipelineDecorator
                >();
                c.RegisterDecorator<
                    IDataPipeline<IRegister<MapNodeData>, MapNodeData>,
                    BucketMapNodePipelineDecorator
                >();
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(RewardNodeDataFinalizerDecorator),
                    xs => xs.ImplementationType == typeof(MapNodeFinalizer)
                );
                c.RegisterSingleton<MapNodeDelegator>();


                RegisterPipeline<CardTraitData, CardTraitDataRegister, CardTraitDataPipeline>(c);

                RegisterPipeline<RewardData, RewardDataRegister, RewardDataPipeline>(c);

                c.Collection.Register<IFactory<RewardData>>(
                    [typeof(CardPoolRewardDataFactory), typeof(DraftRewardDataFactory)],
                    Lifestyle.Singleton
                );
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(CardPoolRewardDataFinalizerDecorator),
                    xs => xs.ImplementationType == typeof(RewardDataFinalizer)
                );
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(DraftRewardDataFinalizerDecorator),
                    xs => xs.ImplementationType == typeof(RewardDataFinalizer)
                );
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(GrantableRewardDataFinalizerDecorator),
                    xs => xs.ImplementationType == typeof(RewardDataFinalizer)
                );

                //Register Relic Data
                RegisterPipeline<RelicData, RelicDataRegister, RelicDataPipeline>(c);

                c.Collection.Register<IFactory<RelicData>>(
                    [
                        typeof(CollectableRelicDataFactory),
                        typeof(EnhancerDataFactory),
                        typeof(MutatorDataFactory),
                        typeof(PyreArtifactDataFactory),
                        typeof(SinsDataFactory),
                        typeof(SoulDataFactory),
                    ],
                    Lifestyle.Singleton
                );

                //CollectableRelicData
                c.RegisterDecorator(
                    typeof(IDataPipeline<IRegister<RelicData>, RelicData>),
                    typeof(CollectableRelicDataPipelineDecorator)
                );
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(CollectableRelicDataFinalizerDecorator),
                    xs => xs.ImplementationType == typeof(RelicDataFinalizer)
                );
                //MutatorData
                c.RegisterDecorator(
                    typeof(IDataPipeline<IRegister<RelicData>, RelicData>),
                    typeof(MutatorDataPipelineDecorator)
                );
                //EnhancerData
                c.RegisterDecorator(
                    typeof(IDataPipeline<IRegister<RelicData>, RelicData>),
                    typeof(EnhancerDataPipelineDecorator)
                );
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(EnhancerDataFinalizerDecorator),
                    xs => xs.ImplementationType == typeof(RelicDataFinalizer)
                );
                // SoulData
                c.RegisterDecorator(
                    typeof(IDataPipeline<IRegister<RelicData>, RelicData>),
                    typeof(SoulDataPipelineDecorator)
                );
                c.RegisterDecorator(
                    typeof(IDataFinalizer),
                    typeof(SoulDataFinalizerDecorator),
                    xs => xs.ImplementationType == typeof(RelicDataFinalizer)
                );


                RegisterPipeline<RelicEffectData, RelicEffectDataRegister, RelicEffectDataPipeline>(c);
                RegisterPipeline<RelicEffectCondition, RelicEffectConditionRegister, RelicEffectConditionPipeline>(c);
                RegisterPipeline<RoomModifierData, RoomModifierRegister, RoomModifierPipeline>(c);
                RegisterPipeline<SpChallengeData, ChallengeDataRegister, ChallengeDataPipeline>(c);

                c.RegisterSingleton<IRegister<BossVariantSpawnData>, BossVariantSpawnRegister>();
                c.RegisterSingleton<BossVariantSpawnRegister, BossVariantSpawnRegister>();

                RegisterPipeline<ScenarioData, ScenarioRegister, ScenarioPipeline>(c);
                c.RegisterSingleton<ScenarioDelegator, ScenarioDelegator>();

                RegisterPipeline<TrialData, TrialDataRegister, TrialDataPipeline>(c);
                c.RegisterSingleton<IRegister<TrialDataList>, TrialListRegister>();
                c.RegisterSingleton<TrialListRegister, TrialListRegister>();

                c.RegisterSingleton<IRegister<BackgroundData>, BackgroundRegister>();
                c.RegisterSingleton<BackgroundRegister, BackgroundRegister>();

                RegisterPipeline<CardUpgradeData, CardUpgradeRegister, CardUpgradePipeline>(c);
                RegisterPipeline<CardUpgradeMaskData, CardUpgradeMaskRegister, CardUpgradeMaskPipeline>(c);
                RegisterPipeline<VfxAtLoc, VfxRegister, VfxPipeline>(c);
            });

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static void RegisterPipeline<DataType, Register, Pipeline>(Container c)
            where Register : class, IRegister<DataType>
            where Pipeline : class, IDataPipeline<IRegister<DataType>, DataType>
        {
            c.RegisterSingleton<IRegister<DataType>, Register>();
            c.RegisterSingleton<Register, Register>();
            c.Register<IDataPipeline<IRegister<DataType>, DataType>, Pipeline>(); 
            c.RegisterInitializer<IRegister<DataType>>(x =>
            {
                var pipeline = c.GetInstance<IDataPipeline<IRegister<DataType>, DataType>>();
                pipeline.Run(x);
            });
        }

        // For debugging only
        private static object? DEBUG_GetInstance(string typeName)
        {
            var type = typeof(VfxRegister).Assembly.FindTypeByClassName(typeName);
            if (type == null) return null;
            return Container.Value.GetInstance(type);
        }
    }
}
