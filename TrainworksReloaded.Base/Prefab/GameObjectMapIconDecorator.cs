﻿using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using ShinyShoe;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using static MultiplayerEmoteDefinitionData;

namespace TrainworksReloaded.Base.Prefab
{
    public class GameObjectMapIconDecorator : IDataPipeline<IRegister<GameObject>, GameObject>
    {
        private readonly IDataPipeline<IRegister<GameObject>, GameObject> decoratee;
        private readonly IRegister<Sprite> spriteRegister;

        public GameObjectMapIconDecorator(
            IDataPipeline<IRegister<GameObject>, GameObject> decoratee,
            IRegister<Sprite> spriteRenderer
        )
        {
            this.decoratee = decoratee;
            this.spriteRegister = spriteRenderer;
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

            var mapConfig = definition
                .Configuration.GetSection("extensions")
                .GetSection("map_node_icon");

            var gameObject = definition.Data;
            var rectTransform = gameObject.AddComponent<RectTransform>();
            var canvasRenderer = gameObject.AddComponent<CanvasRenderer>();
            var mapNodeIcon = gameObject.AddComponent<MapNodeIcon>();
            var raycastTarget = gameObject.AddComponent<Graphic2DInvisRaycastTarget>();

            var artRoot = new GameObject { name = "Art root" };
            artRoot.transform.SetParent(gameObject.transform);

            var selectedIndicator = new GameObject { name = "Selected Indicator" };
            selectedIndicator.transform.SetParent(gameObject.transform);

            AccessTools
                .Field(typeof(MapNodeIcon), "selectedIndicator")
                .SetValue(mapNodeIcon, selectedIndicator);

            var enabled_sprite = mapConfig.GetSection("enabled_sprite").ParseString();
            var enabled_icon = GetIconSprite(definition.Key, enabled_sprite);
            if (enabled_icon != null)
            {
                enabled_icon.transform.SetParent(artRoot.transform);

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Enabled")
                    .SetValue(mapNodeIcon, enabled_icon);
            }

            var visited_sprite_enabled = mapConfig
                .GetSection("visited_sprite_enabled")
                .ParseString();
            var visited_sprite_enabled_icon = GetIconSprite(definition.Key, visited_sprite_enabled);
            if (visited_sprite_enabled_icon != null)
            {
                visited_sprite_enabled_icon.transform.SetParent(artRoot.transform);

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Visited_Enabled")
                    .SetValue(mapNodeIcon, visited_sprite_enabled_icon);
            }

            var visited_sprite_disabled = mapConfig
                .GetSection("visited_sprite_disabled")
                .ParseString();
            var visited_sprite_disabled_icon = GetIconSprite(
                definition.Key,
                visited_sprite_disabled
            );
            if (visited_sprite_disabled_icon != null)
            {
                visited_sprite_disabled_icon.transform.SetParent(artRoot.transform);

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Visited_Disabled")
                    .SetValue(mapNodeIcon, visited_sprite_disabled_icon);
            }

            var disabled_sprite = mapConfig.GetSection("disabled_sprite").ParseString();
            var disabled_sprite_icon = GetIconSprite(definition.Key, disabled_sprite);
            if (disabled_sprite_icon != null)
            {
                disabled_sprite_icon.transform.SetParent(artRoot.transform);

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Visited_Enabled")
                    .SetValue(mapNodeIcon, disabled_sprite_icon);
            }

            var frozen_sprite = mapConfig.GetSection("disabled_sprite").ParseString();
            var frozen_sprite_icon = GetIconSprite(definition.Key, frozen_sprite);
            if (frozen_sprite_icon != null)
            {
                frozen_sprite_icon.transform.SetParent(artRoot.transform);
                var animator = frozen_sprite_icon.GetComponent<Animator>();

                AccessTools
                    .Field(typeof(MapNodeIcon), "frozenAnimator")
                    .SetValue(mapNodeIcon, animator);
            }

            AccessTools
                .Field(typeof(MapNodeIcon), "enabledEmittingParticles")
                .SetValue(mapNodeIcon, new ParticleSystem[0]);
        }

        public GameObject? GetIconSprite(string key, string? spriteStr)
        {
            if (spriteStr == null)
                return null;

            if (
                !spriteRegister.TryLookupId(
                    spriteStr.ToId(key, TemplateConstants.Sprite),
                    out var sprite,
                    out _
                )
            )
                return null;

            var iconSprite = new GameObject { name = $"IconSprite_{spriteStr}" };
            var rectTransform = iconSprite.AddComponent<RectTransform>();

            rectTransform.anchorMin = Vector2.zero; // Bottom-left corner
            rectTransform.anchorMax = Vector2.one; // Top-right corner
            rectTransform.offsetMin = Vector2.zero; // Zero out offsets
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot

            var canvasRenderer = iconSprite.AddComponent<CanvasRenderer>();

            var image = iconSprite.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.SetNativeSize();

            var material = new Material(Shader.Find("Shiny Shoe/CardEffects"))
            {
                mainTexture = sprite.texture,
            };
            material.SetTexture("_Layer1Tex", sprite.texture);
            image.material = material;
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(material, 0);

            var animator = iconSprite.AddComponent<Animator>();
            return iconSprite;
        }
    }
}
