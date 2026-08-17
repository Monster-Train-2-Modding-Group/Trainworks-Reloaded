using HarmonyLib;
using Microsoft.Extensions.Configuration;
using ShinyShoe;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;
using static CharacterUI;

namespace TrainworksReloaded.Base.Prefab
{

    public class GameObjectStoryEventArtFinalizer : IDataFinalizer
    {
        private readonly IModLogger<GameObjectStoryEventArtFinalizer> logger;
        private readonly ICache<IDefinition<GameObject>> cache;
        private readonly Lazy<SaveManager> SaveManager;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<SkeletonDataAsset> skeletonRegister;
        private readonly IDataFinalizer decoratee;

        public GameObjectStoryEventArtFinalizer(
            GameDataClient gameDataClient,
            IModLogger<GameObjectStoryEventArtFinalizer> logger,
            ICache<IDefinition<GameObject>> cache,
            IRegister<Sprite> spriteRegister,
            IRegister<SkeletonDataAsset> skeletonRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
            this.skeletonRegister = skeletonRegister;
            this.decoratee = decoratee;
            SaveManager = new Lazy<SaveManager>(() =>
            {
                if (gameDataClient.TryGetProvider<SaveManager>(out var provider))
                {
                    return provider;
                }
                else
                {
                    return new SaveManager();
                }
            });
        }

        public void FinalizeData()
        {
            var boneDog = SaveManager.Value.GetAllGameData().FindStoryEventDataByName("BoneDogTitan");
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeGameObject(definition);
            }
            decoratee.FinalizeData();
            cache.Clear();
        }

        private void FinalizeGameObject(IDefinition<GameObject> definition)
        {
            var type = definition.Configuration.GetSection("type").Value;

            if (type != "story_event")
                return;

            var characterConfig = definition.Configuration.GetSection("extensions").GetSection("story_event");
        }
    }
}