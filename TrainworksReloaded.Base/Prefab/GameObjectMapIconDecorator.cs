using HarmonyLib;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
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
        private readonly Lazy<Sprite?> indicatorSprite;

        public GameObjectMapIconDecorator(
            IDataPipeline<IRegister<GameObject>, GameObject> decoratee,
            IRegister<Sprite> spriteRenderer
        )
        {
            this.decoratee = decoratee;
            this.spriteRegister = spriteRenderer;
            indicatorSprite = new Lazy<Sprite?>(
                () =>
                    Resources
                        .FindObjectsOfTypeAll<Image>()
                        .FirstOrDefault(xs =>
                        {
                            return xs.name == "Selected indicator";
                        })
                        ?.sprite
            );
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
            gameObject.SetActive(true);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            var canvasRenderer = gameObject.AddComponent<CanvasRenderer>();
            var mapNodeIcon = gameObject.AddComponent<MapNodeIcon>();
            var raycastTarget = gameObject.AddComponent<Graphic2DInvisRaycastTarget>();

            rectTransform.sizeDelta = new Vector2(120, 120);

            var artRoot = new GameObject { name = "Art root" };
            var artRectTransform = artRoot.AddComponent<RectTransform>();
            artRoot.transform.SetParent(gameObject.transform);
            artRectTransform.sizeDelta = new Vector2(210, 210);
            artRectTransform.anchoredPosition = new Vector2(0, 15.2500f);

            var fxRoot = new GameObject { name = "Fx Root" };
            fxRoot.transform.SetParent(gameObject.transform);
            AccessTools.Field(typeof(MapNodeIcon), "enabledFxRoot").SetValue(mapNodeIcon, fxRoot);

            var selectedIndicator = new GameObject { name = "Selected Indicator" };
            var selectedTransform = selectedIndicator.AddComponent<RectTransform>();
            var selectedCanvasRenderer = selectedIndicator.AddComponent<CanvasRenderer>();
            var selectedAspectRatioFilter = selectedIndicator.AddComponent<AspectRatioFitter>();
            selectedAspectRatioFilter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            selectedAspectRatioFilter.aspectRatio = 1.1f;
            selectedTransform.sizeDelta = new Vector2(136.0000f, 123.6364f);
            selectedTransform.anchoredPosition = new Vector2(-1.5f, -16f);
            selectedIndicator.transform.SetParent(gameObject.transform);

            var selectedImage = selectedIndicator.AddComponent<Image>();
            selectedImage.sprite = indicatorSprite.Value;
            AccessTools
                .Field(typeof(MapNodeIcon), "selectedIndicator")
                .SetValue(mapNodeIcon, selectedIndicator);

            var enabled_sprite = mapConfig.GetSection("enabled_sprite").ParseReference();
            var enabled_icon = GetIconSprite(definition.Key, enabled_sprite);
            if (enabled_icon != null)
            {
                enabled_icon.transform.SetParent(artRoot.transform);
                var animator = enabled_icon.AddComponent<Animator>();
                var rect = enabled_icon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero; // Bottom-left corner
                    rect.anchorMax = Vector2.one; // Top-right corner
                    rect.offsetMin = Vector2.zero; // Zero out offsets
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Enabled")
                    .SetValue(mapNodeIcon, enabled_icon);
            }

            var visited_sprite_enabled = mapConfig
                .GetSection("visited_sprite_enabled")
                .ParseReference();
            var visited_sprite_enabled_icon = GetIconSprite(definition.Key, visited_sprite_enabled);
            if (visited_sprite_enabled_icon != null)
            {
                visited_sprite_enabled_icon.transform.SetParent(artRoot.transform);
                var rect = visited_sprite_enabled_icon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero; // Bottom-left corner
                    rect.anchorMax = Vector2.one; // Top-right corner
                    rect.offsetMin = Vector2.zero; // Zero out offsets
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Visited_Enabled")
                    .SetValue(mapNodeIcon, visited_sprite_enabled_icon);
            }

            var visited_sprite_disabled = mapConfig
                .GetSection("visited_sprite_disabled")
                .ParseReference();
            var visited_sprite_disabled_icon = GetIconSprite(
                definition.Key,
                visited_sprite_disabled
            );
            if (visited_sprite_disabled_icon != null)
            {
                visited_sprite_disabled_icon.transform.SetParent(artRoot.transform);
                var rect = visited_sprite_disabled_icon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero; // Bottom-left corner
                    rect.anchorMax = Vector2.one; // Top-right corner
                    rect.offsetMin = Vector2.zero; // Zero out offsets
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Visited_Disabled")
                    .SetValue(mapNodeIcon, visited_sprite_disabled_icon);
            }

            var disabled_sprite = mapConfig.GetSection("disabled_sprite").ParseReference();
            var disabled_sprite_icon = GetIconSprite(definition.Key, disabled_sprite);
            if (disabled_sprite_icon != null)
            {
                disabled_sprite_icon.transform.SetParent(artRoot.transform);
                var rect = disabled_sprite_icon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero; // Bottom-left corner
                    rect.anchorMax = Vector2.one; // Top-right corner
                    rect.offsetMin = Vector2.zero; // Zero out offsets
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(MapNodeIcon), "iconSprite_Disabled")
                    .SetValue(mapNodeIcon, disabled_sprite_icon);
            }

            var frozen_sprite = mapConfig.GetSection("frozen_sprite").ParseReference();
            var frozen_sprite_icon = GetIconSprite(definition.Key, frozen_sprite);
            if (frozen_sprite_icon != null)
            {
                frozen_sprite_icon.transform.SetParent(artRoot.transform);
                var animator = frozen_sprite_icon.GetComponent<Animator>();
                var rect = frozen_sprite_icon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero; // Bottom-left corner
                    rect.anchorMax = Vector2.one; // Top-right corner
                    rect.offsetMin = Vector2.zero; // Zero out offsets
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(MapNodeIcon), "frozenAnimator")
                    .SetValue(mapNodeIcon, animator);
            }

            AccessTools
                .Field(typeof(MapNodeIcon), "enabledEmittingParticles")
                .SetValue(mapNodeIcon, new ParticleSystem[0]);
        }

        public GameObject? GetIconSprite(string key, ReferencedObject? spriteRef)
        {
            if (spriteRef == null)
                return null;

            if (
                !spriteRegister.TryLookupId(
                    spriteRef.ToId(key, TemplateConstants.Sprite),
                    out var sprite,
                    out _,
                    spriteRef.context
                )
            )
            {
                return null;
            }

            var iconSprite = new GameObject { name = $"IconSprite_{sprite.name}" };
            var rectTransform = iconSprite.AddComponent<RectTransform>();

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
