using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ShinyShoe;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Relic;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Prefab
{
    public class GameObjectBattleIconDecorator : IDataPipeline<IRegister<GameObject>, GameObject>
    {
        private readonly IModLogger<GameObjectBattleIconDecorator> logger;
        private readonly IDataPipeline<IRegister<GameObject>, GameObject> decoratee;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly Lazy<Sprite?> indicatorSprite;
        private readonly Lazy<Sprite?> highlightSprite;

        public GameObjectBattleIconDecorator(
            IModLogger<GameObjectBattleIconDecorator> logger,
            IDataPipeline<IRegister<GameObject>, GameObject> decoratee,
            GameDataClient client,
            IRegister<Sprite> spriteRegister
        )
        {
            this.logger = logger;
            this.decoratee = decoratee;
            this.spriteRegister = spriteRegister;
            indicatorSprite = new Lazy<Sprite?>(
                () =>
                    Resources
                        .FindObjectsOfTypeAll<Image>()
                        .FirstOrDefault(xs => { return xs.name == "Selected indicator"; })?.sprite
            );
            // The above doesn't work for getting the Highlight sprite for some reason.
            highlightSprite = new Lazy<Sprite?>(
                () =>
                {
                    client.TryGetProvider<SaveManager>(out var provider);
                    var scenario = provider?.GetAllGameData().FindScenarioDataByName("Level3BattleHeavyHitter_PushAttack");
                    return scenario?.GetMapNodePrefab()?.transform.Find("Art root/Highlight")?.GetComponent<Image>()?.sprite;
                }
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
            if (type != "battle_node_icon")
                return;

            var mapConfig = definition
                .Configuration.GetSection("extensions")
                .GetSection("battle_node_icon");

            var gameObject = definition.Data;
            gameObject.SetActive(true);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            var battleNodeIcon = gameObject.AddComponent<BattleNodeIcon>();
            gameObject.AddComponent<CanvasRenderer>();
            gameObject.AddComponent<Graphic2DInvisRaycastTarget>();

            rectTransform.sizeDelta = new Vector2(120, 120);

            var artRoot = new GameObject { name = "Art root" };
            var artRectTransform = artRoot.AddComponent<RectTransform>();
            artRoot.transform.SetParent(gameObject.transform);
            artRectTransform.sizeDelta = new Vector2(0, 0);
            artRectTransform.anchoredPosition = new Vector2(0, -5f);

            var selectedIndicator = new GameObject { name = "Selected Indicator" };
            var selectedTransform = selectedIndicator.AddComponent<RectTransform>();
            selectedIndicator.AddComponent<CanvasRenderer>();
            selectedTransform.sizeDelta = new Vector2(155.0000f, 140f);
            selectedTransform.anchoredPosition = new Vector2(-1f, 0f);
            selectedIndicator.transform.SetParent(gameObject.transform);

            var selectedImage = selectedIndicator.AddComponent<Image>();
            selectedImage.sprite = indicatorSprite.Value;
            AccessTools
                .Field(typeof(BattleNodeIcon), "selectionIndicator")
                .SetValue(battleNodeIcon, selectedIndicator);

            var highlightGameObject = new GameObject { name = "Highlight" };
            var highlightTransform = highlightGameObject.AddComponent<RectTransform>();
            highlightGameObject.AddComponent<CanvasRenderer>();
            highlightTransform.sizeDelta = new Vector2(136.0000f, 127.0000f);
            highlightTransform.anchoredPosition = new Vector2(0f, 5.7f);
            highlightGameObject.transform.SetParent(artRoot.transform);

            var highlightImage = highlightGameObject.AddComponent<Image>();
            highlightImage.sprite = highlightSprite.Value;
            AccessTools
                .Field(typeof(BattleNodeIcon), "highlight")
                .SetValue(battleNodeIcon, highlightGameObject);

            var completedSprite = mapConfig.GetSection("completed_sprite").ParseReference();
            var completedIcon = GetIconSprite(definition.Key, completedSprite, "Completed");
            if (completedIcon != null)
            {
                completedIcon.transform.SetParent(artRoot.transform);
                var rect = completedIcon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(-77, -82);
                    rect.offsetMax = new Vector2(77, 64);
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(BattleNodeIcon), "completedIcon")
                    .SetValue(battleNodeIcon, completedIcon);
            }

            var activeSprite = mapConfig.GetSection("active_sprite").ParseReference();
            var activeIcon = GetIconSprite(definition.Key, activeSprite, "Active");
            if (activeIcon != null)
            {
                activeIcon.transform.SetParent(artRoot.transform);
                var rect = activeIcon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(-72.5f, -67.5f);
                    rect.offsetMax = new Vector2(72.5f, 67.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(BattleNodeIcon), "activeIcon")
                    .SetValue(battleNodeIcon, activeIcon);
            }

            var inactiveSprite = mapConfig.GetSection("inactive_sprite").ParseReference();
            var inactiveIcon = GetIconSprite(definition.Key, inactiveSprite, "Inactive");
            if (inactiveIcon != null)
            {
                inactiveIcon.transform.SetParent(artRoot.transform);
                var rect = inactiveIcon.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(-72.5f, -67.5f);
                    rect.offsetMax = new Vector2(72.5f, 67.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                }

                AccessTools
                    .Field(typeof(BattleNodeIcon), "inactiveIcon")
                    .SetValue(battleNodeIcon, inactiveIcon);
            }
        }

        public GameObject? GetIconSprite(string key, ReferencedObject? spriteStr, string name)
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
            {
                return null;
            }

            var iconSprite = new GameObject { name = name };
            iconSprite.AddComponent<RectTransform>();
            iconSprite.AddComponent<CanvasRenderer>();
            var image = iconSprite.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.SetNativeSize();

            return iconSprite;
        }
    }
}
