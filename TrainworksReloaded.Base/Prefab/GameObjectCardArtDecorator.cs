using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using static TrainworksReloaded.Base.Extensions.ParseReferenceExtensions;

namespace TrainworksReloaded.Base.Prefab
{
    public class GameObjectCardArtDecorator : IDataPipeline<IRegister<GameObject>, GameObject>
    {
        private readonly IDataPipeline<IRegister<GameObject>, GameObject> decoratee;
        private readonly IRegister<Texture2D> textureRegister;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IModLogger<GameObjectCardArtDecorator> logger;

        private static readonly Lazy<Material> DefaultCardArtMaterial = new(() => Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "CardMaterial_PunkrockReveler"));
        private static readonly FieldInfo LayerTransformsField = AccessTools.Field(typeof(AnimateCardEffects), "_layerTransforms");
        private static readonly FieldInfo TransformPositionField = AccessTools.Field(typeof(CardEffectTransform), "position");
        private static readonly FieldInfo TransformRotationField = AccessTools.Field(typeof(CardEffectTransform), "rotation");
        private static readonly FieldInfo TransformScaleField = AccessTools.Field(typeof(CardEffectTransform), "scale");

        public GameObjectCardArtDecorator(
            IDataPipeline<IRegister<GameObject>, GameObject> decoratee,
            IModLogger<GameObjectCardArtDecorator> logger,
            IRegister<Texture2D> textureRegister,
            IRegister<Sprite> spriteRegister
        )
        {
            this.decoratee = decoratee;
            this.logger = logger;
            this.textureRegister = textureRegister;
            this.spriteRegister = spriteRegister;
            
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
            if (type != "card_art")
                return;

            var config = definition.Configuration.GetSection("extensions").GetSection("card_art");
            if (config.GetSection("base_layer").Exists())
                SetupAnimatedCardArt(definition, config);
            else
                SetupStaticCardArt(definition, config);
        }

        private void SetupAnimatedCardArt(IDefinition<GameObject> definition, IConfiguration config)
        {
            EffectLayer[] layers = new EffectLayer[9];            
            EffectLayer base_layer = ParseEffectLayer(definition.Key, config.GetSection("base_layer"));
            if (base_layer.texture == null)
            {
                logger.Log(LogLevel.Error, $"Base layer does not have a texture set, mod: {definition.Key} id: {definition.Id} path: {config.GetPath()}");
                return;
            }

            int index = 1;
            layers[0] = base_layer;
            foreach (var layerConfig in config.GetSection("layers").GetChildren())
            {
                var effectLayer = ParseEffectLayer(definition.Key, layerConfig);
                layers[index] = effectLayer;
                index++;
            }

            List<EffectTransform> transforms = [];
            foreach (var transformConfig in config.GetSection("transforms").GetChildren())
            {
                var effectTransform = ParseEffectTransform(transformConfig);
                transforms.Add(effectTransform);
            }

            var gameObject = definition.Data;
            gameObject.layer = 5 /* UI */;
            gameObject.AddComponent<AddressableAssetPrefab>();
            gameObject.AddComponent<RectTransform>();

            var cardArt = new GameObject { name = "CardSprite" };
            cardArt.transform.SetParent(gameObject.transform);
            var canvasRenderer = cardArt.AddComponent<CanvasRenderer>();

            var image = cardArt.AddComponent<Image>();
            var texture2d = base_layer.texture;
            image.sprite = Sprite.Create(texture2d, new Rect(0, 0, texture2d.width, texture2d.height), new Vector2(0.5f, 0.5f));

            var rectTransform = cardArt.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero; // Bottom-left corner
                rectTransform.anchorMax = Vector2.one; // Top-right corner
                rectTransform.offsetMin = Vector2.zero; // Zero out offsets
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
            }

            var animateCardEffects = cardArt.AddComponent<AnimateCardEffects>();
            CardEffectTransform[]? cardEffectTransforms = LayerTransformsField.GetValue(animateCardEffects) as CardEffectTransform[];
            var shader = config.GetSection("shader").Value ?? "Shiny Shoe/CardEffects";

            var material = new Material(Shader.Find(shader))
            {
                name = $"CardMaterial_{gameObject.name}"
            };
            material.CopyPropertiesFromMaterial(DefaultCardArtMaterial.Value);

            for (int i = 0; i < layers.Length; i++)
            {
                SetMaterialLayerProperties(material, i + 1, layers[i]);
            }

            image.material = material;
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(material, 0);

            var cardEffectTransformsObject = new GameObject { name = "CardEffectTransforms" };
            cardEffectTransformsObject.transform.SetParent(gameObject.transform);

            foreach (var effectTransform in transforms)
            {
                var obj = new GameObject(effectTransform.name);
                obj.transform.SetParent(cardEffectTransformsObject.transform);
                var transformComponent = obj.AddComponent<CardEffectTransform>();
                TransformPositionField.SetValue(transformComponent, effectTransform.position);
                TransformRotationField.SetValue(transformComponent, effectTransform.rotation);
                TransformScaleField.SetValue(transformComponent, effectTransform.scale);

                foreach (var transformIndex in effectTransform.layers)
                    cardEffectTransforms![transformIndex] = transformComponent;
            }
        }

        private void SetMaterialLayerProperties(Material material, int index, EffectLayer layer)
        {
            string baseStr = $"_Layer{index}";

            material.SetFloat($"{baseStr}Enabled", layer.enabled ? 1 : 0);
            material.SetFloat($"{baseStr}Type", (int)layer.type);
            material.SetFloat($"{baseStr}Stretch", layer.stretch ? 1 : 0);
            material.SetFloat($"{baseStr}Additive", layer.additive ? 1 : 0);

            material.SetTexture($"{baseStr}Tex", layer.texture);
            material.SetTexture($"{baseStr}Motion", layer.motion_texture);
            material.SetTexture($"{baseStr}Mask", layer.mask_texture);
            
            material.SetColor($"{baseStr}ColorTint", layer.tint);

            material.SetVector($"{baseStr}LinearOffset", layer.linear_offset);
            material.SetVector($"{baseStr}LinearSpeed", layer.linear_speed);
            material.SetVector($"{baseStr}Tilt", layer.tilt);
            material.SetVector($"{baseStr}RotationSpeed", layer.rotation_speed);
            material.SetVector($"{baseStr}Scale", layer.scale);
            material.SetVector($"{baseStr}PosOffset", layer.position_offset);
        }

        private EffectLayer ParseEffectLayer(string key, IConfiguration layerConfig)
        {
            EffectLayer ret = new()
            {
                type = ParseType(layerConfig.GetSection("type").Value),
                texture = ParseTexture(key, layerConfig.GetSection("texture").ParseReference()),
                motion_texture = ParseTexture(key, layerConfig.GetSection("motion_texture").ParseReference()),
                mask_texture = ParseTexture(key, layerConfig.GetSection("mask_texture").ParseReference()),
                tint = layerConfig.GetSection("tint").ParseColor() ?? Color.white,
                linear_offset = layerConfig.GetSection("linear_offset").ParseVec2(),
                linear_speed = layerConfig.GetSection("linear_speed").ParseVec2(),
                tilt = layerConfig.GetSection("tilt").ParseVec3(),
                rotation_speed = layerConfig.GetSection("rotation_speed").ParseVec3(),
                scale = layerConfig.GetSection("scale").ParseVec2(1, 1),
                position_offset = layerConfig.GetSection("position_offset").ParseVec2(),
                stretch = layerConfig.GetSection("stretch").ParseBool() ?? true,
                additive = layerConfig.GetSection("additive").ParseBool() ?? true,
                enabled = layerConfig.GetSection("enabled").ParseBool() ?? true,
            };
            return ret;
        }

        private EffectTransform ParseEffectTransform(IConfigurationSection transformConfig)
        {
            return new()
            {
                name = transformConfig.GetSection("id").Value ?? "",
                layers = transformConfig.GetSection("layers").GetChildren().Select(x => x.ParseInt()).Where(x => x != null).Cast<int>().ToArray(),
                position = transformConfig.GetSection("position").ParseVec3(),
                rotation = transformConfig.GetSection("rotation").ParseVec3(),
                scale = transformConfig.GetSection("scale").ParseVec3(1, 1, 1)
            };
        }

        private Texture2D? ParseTexture(string key, ReferencedObject? reference)
        {
            if (reference == null)
                return null;
            textureRegister.TryLookupId(reference.ToId(key, TemplateConstants.Sprite), out var texture, out _, reference.context);
            return texture;
        }

        private CardEffectsMaterial.EffectType ParseType(string? type)
        {
            if (type == null)
                return CardEffectsMaterial.EffectType.None;

            return type.ToLower() switch
            {
                "none" => CardEffectsMaterial.EffectType.None,
                "texture" => CardEffectsMaterial.EffectType.Texture,
                "distortion" => CardEffectsMaterial.EffectType.Distortion,
                "flowing_texture" => CardEffectsMaterial.EffectType.FlowingTexture,
                "moving_texture" => CardEffectsMaterial.EffectType.MovingTexture,
                "dissolve" => CardEffectsMaterial.EffectType.Dissolve,
                "glow" => CardEffectsMaterial.EffectType.Glow,
                _ => CardEffectsMaterial.EffectType.None
            };
        }

        private void SetupStaticCardArt(IDefinition<GameObject> definition, IConfiguration config)
        {
            var spriteVal = config.GetSection("sprite").ParseReference();
            if (spriteVal == null)
                return;

            var id = spriteVal.ToId(definition.Key, TemplateConstants.Sprite);
            if (!spriteRegister.TryLookupId(id, out var sprite, out _, spriteVal.context))
                return;

            var gameObject = definition.Data;
            gameObject.layer = 5;
            gameObject.AddComponent<AddressableAssetPrefab>();
            gameObject.AddComponent<RectTransform>();

            var cardArt = new GameObject { name = "CardSprite" };
            cardArt.transform.SetParent(gameObject.transform);
            var canvasRenderer = cardArt.AddComponent<CanvasRenderer>();

            var image = cardArt.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.SetNativeSize();

            var rectTransform = cardArt.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero; // Bottom-left corner
                rectTransform.anchorMax = Vector2.one; // Top-right corner
                rectTransform.offsetMin = Vector2.zero; // Zero out offsets
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
            }

            var material = new Material(Shader.Find("Shiny Shoe/CardEffects"))
            {
                mainTexture = sprite.texture,
            };
            material.SetTexture("_Layer1Tex", sprite.texture);
            image.material = material;
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(material, 0);

            var cardEffectTransforms = new GameObject { name = "CardEffectTransforms" };
            cardEffectTransforms.transform.SetParent(gameObject.transform);
        }
    }
}
