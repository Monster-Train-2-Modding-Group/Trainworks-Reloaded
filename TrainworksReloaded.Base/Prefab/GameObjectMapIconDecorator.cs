using HarmonyLib;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Prefab
{
    public class GameObjectMapIconDecorator : IDataPipeline<IRegister<GameObject>, GameObject>
    {
        private readonly IDataPipeline<IRegister<GameObject>, GameObject> decoratee;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IModLogger<GameObjectMapIconDecorator> logger;
        private readonly Lazy<RewardNodeData?> baseMapNode;

        public GameObjectMapIconDecorator(
            IDataPipeline<IRegister<GameObject>, GameObject> decoratee,
            IModLogger<GameObjectMapIconDecorator> logger,
            GameDataClient gameDataClient,
            IRegister<Sprite> spriteRenderer
        )
        {
            this.decoratee = decoratee;
            this.spriteRegister = spriteRenderer;
            this.logger = logger;
            baseMapNode = new(() =>
            {
                SaveManager? saveManager;
                if (gameDataClient.TryGetProvider(out saveManager))
                {
                    return saveManager.GetAllGameData().FindMapNodeData(id: /*RewardNodeUnitPackRemnant*/ "904c4de0-5e5a-45c2-af71-dcbebf7bb69a") as RewardNodeData;
                }
                return null;
            });
        }

        public List<IDefinition<GameObject>> Run(IRegister<GameObject> service)
        {
            var definitions = decoratee.Run(service);
            foreach (var definition in definitions)
            {
                Setup(definition);
            }
            return definitions;
        }

        public void Setup(IDefinition<GameObject> definition)
        {
            var type = definition.Configuration.GetSection("type").Value;
            if (type != "map_node_icon")
                return;

            var mapConfig = definition.Configuration.GetSection("extensions").GetSection("map_node_icon");

            var gameObject = definition.Data;
            gameObject.SetActive(true);

            var originalMapIconPrefab = baseMapNode.Value?.GetMapIconPrefab();
            gameObject.CopyPrefabToObject(originalMapIconPrefab!.gameObject);

            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 120);

            var mapNodeIcon = gameObject.GetComponent<MapNodeIcon>();
            mapNodeIcon.useGUILayout = true;

            var fxRoot = gameObject.transform.Find("Enabled FX").gameObject;
            var selectedIndicator = gameObject.transform.Find("Selected indicator").gameObject;
            var enabled_icon = gameObject.transform.Find("Art root/IconSprite_Enabled").gameObject;
            var visited_disabled_icon = gameObject.transform.Find("Art root/IconSprite_Visited_Disabled").gameObject;
            var disabled_icon = gameObject.transform.Find("Art root/IconSprite_Disabled").gameObject;
            var frozen_icon = gameObject.transform.Find("Art root/IconSprite_Frozen").gameObject;
            var animator = frozen_icon.GetComponent<Animator>();

            AccessTools.Field(typeof(MapNodeIcon), "enabledFxRoot").SetValue(mapNodeIcon, fxRoot);
            AccessTools.Field(typeof(MapNodeIcon), "iconSprite_Enabled").SetValue(mapNodeIcon, enabled_icon);
            AccessTools.Field(typeof(MapNodeIcon), "iconSprite_Visited_Enabled").SetValue(mapNodeIcon, null);
            AccessTools.Field(typeof(MapNodeIcon), "iconSprite_Visited_Disabled").SetValue(mapNodeIcon, visited_disabled_icon);
            AccessTools.Field(typeof(MapNodeIcon), "iconSprite_Disabled").SetValue(mapNodeIcon, disabled_icon);
            AccessTools.Field(typeof(MapNodeIcon), "selectedIndicator").SetValue(mapNodeIcon, selectedIndicator);
            AccessTools.Field(typeof(MapNodeIcon), "frozenAnimator").SetValue(mapNodeIcon, animator);
            AccessTools.Field(typeof(MapNodeIcon), "enabledEmittingParticles").SetValue(mapNodeIcon, new ParticleSystem[0]);

            var key = definition.Key;
            ReferencedObject? enabledSpriteRef = mapConfig.GetSection("enabled_sprite").ParseReference();
            ReferencedObject? enabledMaskSpriteRef = mapConfig.GetSection("enabled_mask_sprite").ParseReference();
            ReferencedObject? disabledVisitedSpriteRef = mapConfig.GetSection("visited_sprite_disabled").ParseReference();
            ReferencedObject? disabledSpriteRef = mapConfig.GetSection("disabled_sprite").ParseReference();
            ReferencedObject? frozenSpriteRef = mapConfig.GetSection("frozen_sprite").ParseReference();

            SetupIconSprite(enabled_icon, key, enabledSpriteRef, "_Layer1Tex", enabledMaskSpriteRef, "_Layer4Mask");
            SetupIconSprite(visited_disabled_icon, key, disabledVisitedSpriteRef, "_Layer1Tex");
            SetupIconSprite(disabled_icon, key, disabledSpriteRef, "_Layer1Tex");
            SetupIconSprite(frozen_icon, key, frozenSpriteRef, "_Layer2Tex", frozenSpriteRef, "_Layer2Motion");
        }

        public void SetupIconSprite(GameObject iconSprite, string key, ReferencedObject? spriteRef, string materialPropertyName, ReferencedObject? additionalSprite = null, string? additionalPropertyName=null)
        {
            if (spriteRef == null || !spriteRegister.TryLookupId(spriteRef.ToId(key, TemplateConstants.Sprite), out var sprite, out _, spriteRef.context))
            {
                return;
            }

            Sprite? sprite2 = null;
            if (additionalSprite != null)
            {
                spriteRegister.TryLookupId(additionalSprite.ToId(key, TemplateConstants.Sprite), out sprite2, out _, additionalSprite.context);
            }

            var image = iconSprite.GetComponent<Image>();
            var srcMaterial = new Material(image.material);
            image.material = srcMaterial;
            image.material.SetTexture(materialPropertyName, sprite.texture);
            if (additionalPropertyName != null)
                image.material.SetTexture(additionalPropertyName, sprite2?.texture ?? Texture2D.blackTexture);
        }
    }
}
