﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using Microsoft.Extensions.Configuration;
using ShinyShoe.Logging;
using SimpleInjector;
using TrainworksReloaded.Base;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.CardUpgrade;
using TrainworksReloaded.Base.Character;
using TrainworksReloaded.Base.Class;
using TrainworksReloaded.Base.Effect;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Base.Room;
using TrainworksReloaded.Base.Trait;
using TrainworksReloaded.Base.Trigger;
using TrainworksReloaded.Core;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TrainworksReloaded.Plugin
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = new("TrainworksReloaded");

        internal static Lazy<Container> Container = new(() => Railend.GetContainer());

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
            var client = new GameDataClient();
            DepInjector.AddClient(client);

            // Plugin startup logic
            Logger = base.Logger;
            if (configIncludeGameLogs.Value)
            {
                Log.AddProvider(new ModLogger<Plugin>(Logger));
            }

            //everything rail head
            //var builder = Railhead.GetBuilder();
            //builder.Configure(
            //    "StewardClan",
            //    c =>
            //    {
            //        c.AddJsonFile("plugin.json");
            //    }
            //);
            //builder.Configure(
            //    "FireStarter",
            //    c =>
            //    {
            //        c.AddJsonFile("fire_starter.json");
            //    }
            //);

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
                        typeof(CardDataFinalizer),
                        typeof(CardEffectFinalizer),
                        typeof(CardTraitDataFinalizer),
                        typeof(CardUpgradeFinalizer),
                        typeof(CharacterDataFinalizer),
                        typeof(ClassDataFinalizer),
                        typeof(CharacterTriggerFinalizer),
                        typeof(CardTriggerEffectFinalizer),
                        typeof(VfxFinalizer),
                        typeof(RoomModifierFinalizer),
                    ]
                );

                //Register Localization
                c.RegisterSingleton<IRegister<LocalizationTerm>, CustomLocalizationTermRegistry>();
                c.RegisterSingleton<
                    CustomLocalizationTermRegistry,
                    CustomLocalizationTermRegistry
                >();

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

                c.RegisterDecorator<
                    IDataPipeline<IRegister<CardData>, CardData>,
                    PoolingCardDataPipelineDecorator
                >();

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
                    GameObjectCardArtSetup
                >();

                //Register Sprite
                c.RegisterSingleton<IRegister<Sprite>, SpriteRegister>();
                c.RegisterSingleton<SpriteRegister, SpriteRegister>();
                c.Register<IDataPipeline<IRegister<Sprite>, Sprite>, SpritePipeline>();
                c.RegisterInitializer<IRegister<Sprite>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<Sprite>, Sprite>>();
                    pipeline.Run(x);
                });

                //Register Card Data
                c.RegisterSingleton<IRegister<CardData>, CardDataRegister>(); //a place to register and access custom card data
                c.RegisterSingleton<CardDataRegister, CardDataRegister>();
                c.Register<IDataPipeline<IRegister<CardData>, CardData>, CardDataPipeline>(); //a data pipeline to run as soon as register is needed
                c.RegisterInitializer<IRegister<CardData>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<CardData>, CardData>>();
                    pipeline.Run(x);
                });
                c.RegisterSingleton<CardPoolDelegator>();

                //Register Character Data
                c.RegisterSingleton<IRegister<CharacterData>, CharacterDataRegister>(); //a place to register and access custom card data
                c.RegisterSingleton<CharacterDataRegister, CharacterDataRegister>();
                c.Register<
                    IDataPipeline<IRegister<CharacterData>, CharacterData>,
                    CharacterDataPipeline
                >(); //a data pipeline to run as soon as register is needed
                c.RegisterInitializer<IRegister<CharacterData>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<CharacterData>, CharacterData>
                    >();
                    pipeline.Run(x);
                });

                //Register Card Trigger
                c.RegisterSingleton<IRegister<CardTriggerEffectData>, CardTriggerEffectRegister>();
                c.RegisterSingleton<CardTriggerEffectRegister, CardTriggerEffectRegister>();
                c.Register<
                    IDataPipeline<IRegister<CardTriggerEffectData>, CardTriggerEffectData>,
                    CardTriggerEffectPipeline
                >();
                c.RegisterInitializer<IRegister<CardTriggerEffectData>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<CardTriggerEffectData>, CardTriggerEffectData>
                    >();
                    pipeline.Run(x);
                });

                //Register Character Trigger
                c.RegisterSingleton<IRegister<CharacterTriggerData>, CharacterTriggerRegister>(); //a place to register and access custom card data
                c.RegisterSingleton<CharacterTriggerRegister, CharacterTriggerRegister>();
                c.Register<
                    IDataPipeline<IRegister<CharacterTriggerData>, CharacterTriggerData>,
                    CharacterTriggerPipeline
                >(); //a data pipeline to run as soon as register is needed
                c.RegisterInitializer<IRegister<CharacterTriggerData>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<CharacterTriggerData>, CharacterTriggerData>
                    >();
                    pipeline.Run(x);
                });

                //Register Class Data
                c.RegisterSingleton<IRegister<ClassData>, ClassDataRegister>();
                c.RegisterSingleton<ClassDataRegister, ClassDataRegister>();
                c.Register<IDataPipeline<IRegister<ClassData>, ClassData>, ClassDataPipeline>();
                c.RegisterInitializer<IRegister<ClassData>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<ClassData>, ClassData>>();
                    pipeline.Run(x);
                });

                //Register Effect Data
                c.RegisterSingleton<IRegister<CardEffectData>, CardEffectDataRegister>();
                c.RegisterSingleton<CardEffectDataRegister, CardEffectDataRegister>();
                c.Register<
                    IDataPipeline<IRegister<CardEffectData>, CardEffectData>,
                    CardEffectDataPipeline
                >();
                c.RegisterInitializer<IRegister<CardEffectData>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<CardEffectData>, CardEffectData>
                    >();
                    pipeline.Run(x);
                });

                //Register Trait Data
                c.RegisterSingleton<IRegister<CardTraitData>, CardTraitDataRegister>();
                c.RegisterSingleton<CardTraitDataRegister, CardTraitDataRegister>();
                c.Register<
                    IDataPipeline<IRegister<CardTraitData>, CardTraitData>,
                    CardTraitDataPipeline
                >();
                c.RegisterInitializer<IRegister<CardTraitData>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<CardTraitData>, CardTraitData>
                    >();
                    pipeline.Run(x);
                });

                //Register Room Modifier Data
                c.RegisterSingleton<IRegister<RoomModifierData>, RoomModifierRegister>();
                c.RegisterSingleton<RoomModifierRegister, RoomModifierRegister>();
                c.Register<
                    IDataPipeline<IRegister<RoomModifierData>, RoomModifierData>,
                    RoomModifierPipeline
                >();
                c.RegisterInitializer<IRegister<RoomModifierData>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<RoomModifierData>, RoomModifierData>
                    >();
                    pipeline.Run(x);
                });

                //Register Upgrade Data
                c.RegisterSingleton<IRegister<CardUpgradeData>, CardUpgradeRegister>();
                c.RegisterSingleton<CardUpgradeRegister, CardUpgradeRegister>();
                c.Register<
                    IDataPipeline<IRegister<CardUpgradeData>, CardUpgradeData>,
                    CardUpgradePipeline
                >();
                c.RegisterInitializer<IRegister<CardUpgradeData>>(x =>
                {
                    var pipeline = c.GetInstance<
                        IDataPipeline<IRegister<CardUpgradeData>, CardUpgradeData>
                    >();
                    pipeline.Run(x);
                });

                //Register Vfx
                c.RegisterSingleton<IRegister<VfxAtLoc>, VfxRegister>();
                c.RegisterSingleton<VfxRegister, VfxRegister>();
                c.Register<IDataPipeline<IRegister<VfxAtLoc>, VfxAtLoc>, VfxPipeline>();
                c.RegisterInitializer<IRegister<VfxAtLoc>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<VfxAtLoc>, VfxAtLoc>>();
                    pipeline.Run(x);
                });
            });

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
