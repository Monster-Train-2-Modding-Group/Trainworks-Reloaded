﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using Microsoft.Extensions.Configuration;
using SimpleInjector;
using TrainworksReloaded.Base;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.CardUpgrade;
using TrainworksReloaded.Base.Character;
using TrainworksReloaded.Base.Class;
using TrainworksReloaded.Base.Effect;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Base.Trait;
using TrainworksReloaded.Core;
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

            //everything rail head
            var builder = Railhead.GetBuilder();
            builder.Configure(
                MyPluginInfo.PLUGIN_GUID,
                c =>
                {
                    c.AddJsonFile("plugin.json");
                }
            );

            Railend.ConfigurePreAction(c =>
            {
                //Register for Logging
                c.RegisterInstance(Logger);

                //Register hooking into games dependency injection system
                c.RegisterInstance(client);

                //Register Assets
                c.RegisterSingleton<IRegister<GameObject>, GameObjectRegister>();
                c.RegisterSingleton<GameObjectRegister, GameObjectRegister>();
                c.RegisterInitializer<GameObjectRegister>(x =>
                {
                    Addressables.ResourceLocators.Add(x);
                });
                c.RegisterInitializer<IRegister<GameObject>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<GameObject>>>();
                    pipeline.Run(x);
                });
                c.Register<IDataPipeline<IRegister<GameObject>>, GameObjectImportPipeline>(); //a data pipeline to run as soon as register is needed
                c.Collection.Append<IDataPipelineSetup<GameObject>, GameObjectCardArtSetup>(
                    Lifestyle.Singleton
                );
                c.RegisterSingleton<IRegister<Sprite>, SpriteRegister>();
                c.RegisterSingleton<SpriteRegister, SpriteRegister>();
                c.Register<IDataPipeline<IRegister<Sprite>>, SpritePipeline>();
                c.RegisterInitializer<IRegister<Sprite>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<Sprite>>>();
                    pipeline.Run(x);
                });

                //Register Card Data
                c.RegisterSingleton<IRegister<CardData>, CardDataRegister>(); //a place to register and access custom card data
                c.RegisterSingleton<CardDataRegister, CardDataRegister>();
                c.Register<IDataPipeline<IRegister<CardData>>, CardDataPipeline>(); //a data pipeline to run as soon as register is needed
                c.Collection.Register<IDataPipelineSetup<CardData>>(new List<Type>());
                c.Collection.Register<IDataPipelineFinalizer<CardData>>(new List<Type>());
                c.RegisterInitializer<IRegister<CardData>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<CardData>>>();
                    pipeline.Run(x);
                });

                //Register Character Data
                c.RegisterSingleton<IRegister<CharacterData>, CharacterDataRegister>(); //a place to register and access custom card data
                c.RegisterSingleton<CharacterDataRegister, CharacterDataRegister>();
                c.Register<IDataPipeline<IRegister<CharacterData>>, CharacterDataPipeline>(); //a data pipeline to run as soon as register is needed
                c.Collection.Register<IDataPipelineSetup<CharacterData>>(new List<Type>());
                c.Collection.Register<IDataPipelineFinalizer<CharacterData>>(new List<Type>());
                c.RegisterInitializer<IRegister<CharacterData>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<CharacterData>>>();
                    pipeline.Run(x);
                });

                //Register Class Data
                c.RegisterSingleton<IRegister<ClassData>, ClassDataRegister>();
                c.RegisterSingleton<ClassDataRegister, ClassDataRegister>();

                //Register Effect Data
                c.RegisterSingleton<IRegister<CardEffectData>, CardEffectDataRegister>();
                c.RegisterSingleton<CardEffectDataRegister, CardEffectDataRegister>();
                c.Register<IDataPipeline<IRegister<CardEffectData>>, CardEffectDataPipeline>();
                c.Collection.Register<IDataPipelineSetup<CardEffectData>>(new List<Type>());
                c.Collection.Register<IDataPipelineFinalizer<CardEffectData>>(new List<Type>());
                c.RegisterInitializer<IRegister<CardEffectData>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<CardEffectData>>>();
                    pipeline.Run(x);
                });

                //Register Trait Data
                c.RegisterSingleton<IRegister<CardTraitData>, CardTraitDataRegister>();
                c.RegisterSingleton<CardTraitDataRegister, CardTraitDataRegister>();
                c.Register<IDataPipeline<IRegister<CardTraitData>>, CardTraitDataPipeline>();
                c.Collection.Register<IDataPipelineSetup<CardTraitData>>(new List<Type>());
                c.Collection.Register<IDataPipelineFinalizer<CardTraitData>>(new List<Type>());
                c.RegisterInitializer<IRegister<CardTraitData>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<CardTraitData>>>();
                    pipeline.Run(x);
                });

                //Register Upgrade Data
                c.RegisterSingleton<IRegister<CardUpgradeData>, CardUpgradeRegister>();
                c.RegisterSingleton<CardUpgradeRegister, CardUpgradeRegister>();
                c.Register<IDataPipeline<IRegister<CardUpgradeData>>, CardUpgradePipeline>();
                c.Collection.Register<IDataPipelineSetup<CardUpgradeData>>(new List<Type>());
                c.Collection.Register<IDataPipelineFinalizer<CardUpgradeData>>(new List<Type>());
                c.RegisterInitializer<IRegister<CardUpgradeData>>(x =>
                {
                    var pipeline = c.GetInstance<IDataPipeline<IRegister<CardUpgradeData>>>();
                    pipeline.Run(x);
                });

                //Register Localization
                c.RegisterSingleton<IRegister<LocalizationTerm>, CustomLocalizationTermRegistry>();
                c.RegisterSingleton<
                    CustomLocalizationTermRegistry,
                    CustomLocalizationTermRegistry
                >();

                c.RegisterConditional(
                    typeof(IModLogger<>),
                    typeof(ModLogger<>),
                    Lifestyle.Singleton,
                    c => !c.Handled
                );
            });

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
