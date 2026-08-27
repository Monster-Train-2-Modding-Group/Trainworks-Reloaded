using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
        private readonly IRegister<AssetBundle> assetBundleRegister;
        private readonly PluginAtlas atlas;
        private readonly GameDataClient gameDataClient;

        private static readonly Lazy<Material> DefaultCardArtMaterial = new(() => Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "CardMaterial_PunkrockReveler"));
        private static readonly FieldInfo LayerTransformsField = AccessTools.Field(typeof(AnimateCardEffects), "_layerTransforms");
        private static readonly FieldInfo FieldsTurnedOnField = AccessTools.Field(typeof(AnimateCardEffects), "_fieldsTurnedOn");
        private static readonly FieldInfo TransformPositionField = AccessTools.Field(typeof(CardEffectTransform), "position");
        private static readonly FieldInfo TransformRotationField = AccessTools.Field(typeof(CardEffectTransform), "rotation");
        private static readonly FieldInfo TransformScaleField = AccessTools.Field(typeof(CardEffectTransform), "scale");
        private static readonly List<Image> RequiresNonAdditiveShader = [];
        private GameObject? CardArt_Spell_Begone;

        public GameObjectCardArtDecorator(
            IDataPipeline<IRegister<GameObject>, GameObject> decoratee,
            IModLogger<GameObjectCardArtDecorator> logger,
            IRegister<Texture2D> textureRegister,
            IRegister<Sprite> spriteRegister,
            IRegister<AssetBundle> assetBundleRegister,
            GameDataClient gameDataClient,
            PluginAtlas atlas
        )
        {
            this.decoratee = decoratee;
            this.logger = logger;
            this.textureRegister = textureRegister;
            this.spriteRegister = spriteRegister;
            this.assetBundleRegister = assetBundleRegister;
            this.atlas = atlas;
            this.gameDataClient = gameDataClient;
        }

        public List<IDefinition<GameObject>> Run(IRegister<GameObject> service)
        {
            var definitions = decoratee.Run(service);
            foreach (var definition in definitions)
            {
                Setup(definition);
            }
            if (RequiresNonAdditiveShader.Count > 0)
            {
                if (gameDataClient.TryGetProvider<SaveManager>(out var saveManager))
                {
                    var card = saveManager.GetAllGameData().FindCardDataByName("Begone");
                    List<AssetReference> assets = [];
                    card?.GetAddressableAssets(assets);
                    if (!assets.IsNullOrEmpty())
                    {
                        var handle = assets[0].LoadAsset<GameObject>();
                        handle.Completed += op =>
                        {
                            if (op.Status != AsyncOperationStatus.Succeeded)
                            {
                                logger.Log(LogLevel.Error, "Failed to load addressable: " + assets[0].RuntimeKey.ToString());
                                return;
                            }

                            CardArt_Spell_Begone = op.Result;
                            var shader = CardArt_Spell_Begone?.transform.Find("CardSprite").GetComponent<Image>().material.shader;
                            if (shader == null)
                            {
                                logger.Log(LogLevel.Error, "Non-Additive shader not found, can not update materials to use the shader.");
                                return;
                            }
                            foreach (var image in RequiresNonAdditiveShader)
                            {
                                var oldMaterial = image.material;
                                image.material = new Material(shader);
                                image.material.CopyPropertiesFromMaterial(oldMaterial);

                            }
                            logger.Log(LogLevel.Info, $"Successfully updated {RequiresNonAdditiveShader.Count} material's shaders");
                        };
                    }
                    else
                    {
                        logger.Log(LogLevel.Error, "Could not find Begone's Addressable Assets");
                    }
                }
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
                SetupLayeredCardArt(definition, config);
            else
                SetupSpriteBasedCardArt(definition, config);

            SetupAnimation(definition, config, definition.Data);
        }

        private void SetupLayeredCardArt(IDefinition<GameObject> definition, IConfiguration config)
        {
            EffectLayer[] layers = new EffectLayer[9];
            int[] fieldsTurnedOn = new int[8];
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
                fieldsTurnedOn[index - 1] = effectLayer.animated ? 1 : 0;
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

            var cardArt = new GameObject { name = "CardSprite", layer = 5 };
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
            FieldsTurnedOnField.SetValue(animateCardEffects, fieldsTurnedOn);
            var shader = config.GetSection("shader").Value ?? "Shiny Shoe/CardEffects";
            bool requiresNonAdditive = false;
            if (shader == "Shiny Shoe/CardEffects Non-Additive")
            {
                // The shader isn't loaded at creation time so create with the normal shader and deferred reassignment til later.
                requiresNonAdditive = true;
                shader = "Shiny Shoe/CardEffects";
            }

            var material = new Material(Shader.Find(shader))
            {
                name = $"CardMaterial_{gameObject.name}"
            };
            if (requiresNonAdditive)
            {
                RequiresNonAdditiveShader.Add(image);
            }
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

        private void SetupAnimation(IDefinition<GameObject> definition, IConfiguration config, GameObject gameObject)
        {
            var cardArt = gameObject.transform.Find("CardSprite").gameObject;
            var image = cardArt.GetComponent<Image>();

            (AnimationClip? animationClip, List<Sprite>? sprites) = ParseAnimationClip(config.GetSection("animation_clip"), definition.Key);
            if (animationClip != null)
            {
                Animation animation = gameObject.GetComponent<Animation>() ?? gameObject.AddComponent<Animation>();
                if (sprites != null)
                {
                    var driver = cardArt.AddComponent<CardSpriteFrameDriver>();
                    var transform = cardArt.AddComponent<CardEffectTransform>();
                    driver.Frames = [.. sprites];
                    driver.TargetImage = image;
                    driver.FrameHolder = transform;
                }

                animation.AddClip(animationClip, animationClip.name);
                var state = animation[animationClip.name];
                if (state != null)
                {
                    state.wrapMode = WrapMode.Loop;
                    state.enabled = true;
                    state.weight = 1f;
                }
                animation.clip = animationClip;
                animation.wrapMode = animationClip.wrapMode;

            }
        }

        private (AnimationClip?, List<Sprite>?) ParseAnimationClip(IConfigurationSection configuration, string key)
        {
            if (!configuration.Exists()) 
                return (null, null);

            AnimationClip animation = new()
            {
                name = configuration.GetSection("id").ParseString() ?? "",
                frameRate = configuration.GetSection("sample_rate").ParseFloat() ?? 60f,
                wrapMode = configuration.GetSection("loop_time").ParseString() == "loop" ? WrapMode.Loop : WrapMode.Once,
                legacy = true
            };
            

            Assembly baseGame = typeof(CardEffectSacrifice).Assembly;

            foreach (var config in configuration.GetSection("curves").GetChildren())
            {
                string path = config.GetSection("path").ParseString() ?? "";
                string typeStr = config.GetSection("type").ParseString() ?? "";
                Type type = baseGame.GetType(typeStr);
                string attribute = config.GetSection("attribute").ParseString() ?? "";
                var curve = new AnimationCurve();
                foreach (var kfconfig in config.GetSection("keyframes").GetChildren())
                {
                    var time = kfconfig.GetSection("time").ParseFloat() ?? 0f;
                    var value = kfconfig.GetSection("value").ParseFloat() ?? 0f;
                    var in_tangent = kfconfig.GetSection("in_tangent").ParseFloat() ?? 0;
                    var out_tangent = kfconfig.GetSection("out_tangent").ParseFloat() ?? 0;
                    Keyframe kf = new(time, value, in_tangent, out_tangent);
                    curve.AddKey(kf);
                }
                animation.SetCurve(path, type, attribute, curve);
            }

            var spriteReferences = configuration.GetSection("frames").ParseReferences();
            List<Sprite>? sprites = [];
            foreach (var reference in spriteReferences)
            {
                if (spriteRegister.TryLookupName(reference!.ToId(key, TemplateConstants.Sprite), out var sprite, out var _, reference.context))
                {
                    sprites.Add(sprite);
                }
            }

            float frameDuration = 1f / animation.frameRate;
            if (!sprites.IsNullOrEmpty())
            {
                var curve = new AnimationCurve();
                for (int i = 0; i < sprites.Count; i++)
                {
                    curve.AddKey(new Keyframe(i * frameDuration, (float)i, 0f, float.PositiveInfinity));
                }
                curve.AddKey(new Keyframe(sprites.Count * frameDuration, (float)(sprites.Count - 1), 0f, 0f));
                curve.preWrapMode = WrapMode.Loop;
                curve.postWrapMode = WrapMode.Loop;
                /// Sigh this is such a stupid hack, Can't use a custom class here because Unity Monobehaviours have to be registered
                /// No way to register a new Script class to use here from a mod so we use a class with a float parameter from within
                /// the base game we can change through this here.
                animation.SetCurve("CardSprite", typeof(CardEffectTransform), "position.x", curve);
                animation.wrapMode = WrapMode.Loop;
            }

            return (animation, sprites);
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
                animated = layerConfig.GetSection("animated").ParseBool() ?? false,
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

        private void SetupSpriteBasedCardArt(IDefinition<GameObject> definition, IConfiguration config)
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

            /*var material = new Material(Shader.Find("Shiny Shoe/CardEffects"))
            {
                mainTexture = sprite.texture,
            };
            material.SetTexture("_Layer1Tex", sprite.texture);
            image.material = material;
            canvasRenderer.materialCount = 1;
            canvasRenderer.SetMaterial(material, 0);

            var cardEffectTransforms = new GameObject { name = "CardEffectTransforms" };
            cardEffectTransforms.transform.SetParent(gameObject.transform);*/
        }
    }
}
