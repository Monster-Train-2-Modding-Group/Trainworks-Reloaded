using HarmonyLib;
using Microsoft.Extensions.Configuration;
using ShinyShoe;
using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;
using static CharacterUI;
using static MultiplayerEmoteDefinitionData;
using static RotaryHeart.Lib.DataBaseExample;

namespace TrainworksReloaded.Base.Prefab
{

    public class GameObjectCharacterArtFinalizer : IDataFinalizer
    {
        private readonly IModLogger<GameObjectCharacterArtFinalizer> logger;
        private readonly ICache<IDefinition<GameObject>> cache;
        private readonly FallbackDataProvider fallbackDataProvider;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IRegister<SkeletonDataAsset> skeletonRegister;
        private readonly IDataFinalizer decoratee;
        private static Material? defaultQuadMaterial;

        private static readonly Dictionary<CharacterUI.Anim, string> ANIM_NAMES = new()
        {
            {
                CharacterUI.Anim.Idle,
                "Idle"
            },
            {
                CharacterUI.Anim.Attack,
                "Attack"
            },
            {
                CharacterUI.Anim.HitReact,
                "HitReact"
            },
            {
                CharacterUI.Anim.Idle_Relentless,
                "Idle_Relentless"
            },
            {
                CharacterUI.Anim.Attack_Spell,
                "Spell"
            },
            {
                CharacterUI.Anim.Death,
                "Death"
            },
            {
                CharacterUI.Anim.Talk,
                "Talk"
            },
            {
                CharacterUI.Anim.Hover,
                "Hover"
            }
        };

        public GameObjectCharacterArtFinalizer(
            IModLogger<GameObjectCharacterArtFinalizer> logger,
            ICache<IDefinition<GameObject>> cache,
            FallbackDataProvider fallbackDataProvider,
            IRegister<Sprite> spriteRegister,
            IRegister<SkeletonDataAsset> skeletonRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.fallbackDataProvider = fallbackDataProvider;
            this.spriteRegister = spriteRegister;
            this.skeletonRegister = skeletonRegister;
            this.decoratee = decoratee;
        }

        public void FinalizeData()
        {
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
            if (type != "character_art")
                return;

            var characterConfig = definition
                .Configuration.GetSection("extensions")
                .GetSection("character_art");

            // Get Required Sprite
            var spriteVal = characterConfig.GetSection("sprite").ParseReference();
            if (spriteVal == null)
            {
                logger.Log(LogLevel.Warning, $"For GameObject with Id: {definition.Id} did not find a required field sprite for it.");
                return;
            }

            var id = spriteVal.ToId(definition.Key, TemplateConstants.Sprite);
            if (!spriteRegister.TryLookupId(id, out var sprite, out _, spriteVal.context))
            {
                return;
            }
                
            // Get Optional Skeleton Animation configuration.
            Dictionary<CharacterUI.Anim, SkeletonDataAsset> animations = [];
            foreach (var animationConfig in characterConfig.GetSection("skeleton_animations").GetChildren())
            {
                var anim = animationConfig.GetSection("animation").ParseAnim();
                var skeletonReference = animationConfig.GetSection("skeleton").ParseReference();
                if (anim == null || skeletonReference == null)
                {
                    logger.Log(LogLevel.Warning, $"Skipping {definition.Key} {definition.Id} skeleton data animation: {anim} skeleton: {skeletonReference?.id}");
                    continue;
                }
                var skeletonId = skeletonReference.ToId(definition.Key, TemplateConstants.SkeletonData);
                if (!skeletonRegister.TryLookupId(skeletonId, out var skeleton, out _, skeletonReference.context))
                    continue;

                animations.Add(anim.Value, skeleton);
            }
            bool usingAnimations = animations.Count > 0;

            // Clone the Fallback Character fields onto our GameObject.
            GameObject original = definition.Data;
            var fallbackData = fallbackDataProvider.FallbackData;
            var prefab = fallbackData.GetDefaultCharacterPrefab();
            CopyFallbackPrefab(original, prefab);

            if (usingAnimations)
            {
                CreateCharacterWithSkeletonAnimations(original, definition.Id, sprite, animations, characterConfig);
            }
            else
            {
                CreateCharacterWithStaticSprite(original, definition.Id, sprite, characterConfig);
            }
            PostCharacterAdjustments(original, sprite, characterConfig);
        }

        private void CreateCharacterWithStaticSprite(GameObject original, string name, Sprite sprite, IConfiguration configuration)
        {   
            var spriteRenderer = original.transform.Find("CharacterScale/CharacterUI")?.GetComponent<SpriteRenderer>();
            var quadDefault = original.transform.Find("CharacterScale/CharacterUI/Quad_Default");
            var meshRenderer = quadDefault?.GetComponent<MeshRenderer>();
            var characterUIMesh = quadDefault?.GetComponent<CharacterUIMesh>();
            var spineMeshesObject = original.transform.Find("CharacterScale/CharacterUI/SpineMeshes");
            var characterUIMeshSpine = spineMeshesObject?.GetComponent<CharacterUIMeshSpine>();

            // Validate required components
            if (spriteRenderer == null || meshRenderer == null || characterUIMesh == null || characterUIMeshSpine == null || spineMeshesObject == null)
            {
                logger.Log(LogLevel.Error, $"Missing required components on prefab for {name}");
                return;
            }

            // Destroy and deactivate the CharacterUIMeshSpine as its not needed.
            GameObject.Destroy(characterUIMeshSpine);
            spineMeshesObject.gameObject.SetActive(false);

            characterUIMesh.Setup(sprite, -1f, name, out var _);
            
            // Setup mesh renderer.
            var shaderConfig = configuration.GetSection("shader");
            // The actual default was "Shiny Shoe/Character Shader", but that shader requires light to display properly.
            var shaderName = shaderConfig.GetSection("name").Value ?? "Sprites/Default";
            var characterShader = Shader.Find(shaderName);
            if (characterShader == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find shader {shaderName} for {name}");
                return;
            }

            // Get the default material that is on QuadDefault. This should be true for the Fallback character.
            if (defaultQuadMaterial == null)
                defaultQuadMaterial = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "CharacterMaterial_Default");

            var material = new Material(characterShader);
            CopyMaterialProperties(defaultQuadMaterial, material);

            // Handle color configuration
            var color = GetColorFromSection(shaderConfig.GetSection("color"));
            TrySetMaterialColor(material, "_Color", color);
            var tint = GetColorFromSection(shaderConfig.GetSection("tint"));
            TrySetMaterialColor(material, "_Tint", tint);

            meshRenderer.material = material;

            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
        }

        private static void CopyMaterialProperties(Material srcmat, Material dstmat)
        {
            Shader shader = srcmat.shader;
            int count = shader.GetPropertyCount();

            for (int i = 0; i < count; i++)
            {
                string name = shader.GetPropertyName(i);
                var type = shader.GetPropertyType(i);

                switch (type)
                {
                    case ShaderPropertyType.Color:
                        dstmat.SetColor(name, srcmat.GetColor(name));
                        break;
                    case ShaderPropertyType.Vector:
                        dstmat.SetVector(name, srcmat.GetVector(name));
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        dstmat.SetFloat(name, srcmat.GetFloat(name));
                        break;
                    case ShaderPropertyType.Texture:
                        dstmat.SetTexture(name, srcmat.GetTexture(name));
                        break;
                }
            }
        }

        public static void PrintMaterialProperties(Material mat)
        {
            if (mat == null)
            {
                Debug.Log("Material is null");
                return;
            }

            // Note only prints propertries that the shader expects.
            Shader shader = mat.shader;
            int count = shader.GetPropertyCount();

            Debug.Log($"--- Material: {mat.name}, Shader: {shader.name} ---");

            for (int i = 0; i < count; i++)
            {
                string name = shader.GetPropertyName(i);
                var type = shader.GetPropertyType(i);

                string valueStr = GetMaterialValueString(mat, i, type, name);

                Debug.Log($"{name} ({type}) = {valueStr}");
            }
        }

        private static string GetMaterialValueString(Material mat, int index, ShaderPropertyType type, string name)
        {
            switch (type)
            {
                case ShaderPropertyType.Color:
                    return mat.GetColor(name).ToString();

                case ShaderPropertyType.Vector:
                    return mat.GetVector(name).ToString();

                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    return mat.GetFloat(name).ToString();

                case ShaderPropertyType.Texture:
                    Texture tex = mat.GetTexture(name);
                    return tex != null ? tex.name : "null";

                default:
                    return "(unknown type)";
            }
        }

        private void CreateCharacterWithSkeletonAnimations(GameObject original, string name, Sprite sprite, Dictionary<Anim, SkeletonDataAsset> animations, IConfiguration configuration)
        {
            var spriteRenderer = original.transform.Find("CharacterScale/CharacterUI")?.GetComponent<SpriteRenderer>();
            var quadDefault = original.transform.Find("CharacterScale/CharacterUI/Quad_Default");
            var spineMeshesObject = original.transform.Find("CharacterScale/CharacterUI/SpineMeshes");
            var characterUIMeshSpine = spineMeshesObject?.GetComponent<CharacterUIMeshSpine>();

            // Validate required components
            if (spriteRenderer == null || quadDefault == null || characterUIMeshSpine == null || spineMeshesObject == null)
            {
                logger.Log(LogLevel.Error, $"Missing required components on prefab for {name}");
                return;
            }

            foreach (var anim_skeleton in animations)
            {
                var anim = anim_skeleton.Key;
                var skeleton = anim_skeleton.Value;

                SkeletonAnimation animation = SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
                animation.name = "Spine GameObject (" + skeleton.name + " " + anim.ToString() + ")";
                animation.transform.SetParent(spineMeshesObject);
                animation.gameObject.layer = LayerMask.NameToLayer("Character_Lights");
                animation.transform.localPosition = Vector3.zero;
                animation.AnimationState.SetAnimation(0, ANIM_NAMES[anim], true);
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
            quadDefault.gameObject.SetActive(false);
            characterUIMeshSpine.gameObject.SetActive(true);
            characterUIMeshSpine.Setup(sprite, -1f, name, out var _);
        }

        private void PostCharacterAdjustments(GameObject original, Sprite sprite, IConfiguration configuration)
        {
            var characterState = original.GetComponent<CharacterState>();
            var characterUIObject = original.transform.Find("CharacterScale/CharacterUI");
            var characterUI = original.transform.Find("CharacterScale/CharacterUI")?.GetComponent<CharacterUI>();
            var unitAbilityIconUI = original.transform.Find("DetailsUIRoot/BottomAnchor/Stats/AbilityAndTriggersGroup/UnitAbilityUI")?.GetComponent<UnitAbilityIconUI>();

            // Bypass calling CharacterState.InitialSetup
            AccessTools.Field(typeof(CharacterState), "sprite").SetValue(characterState, sprite);
            AccessTools.Field(typeof(CharacterState), "charUI").SetValue(characterState, characterUI);
            AccessTools.Field(typeof(UnitAbilityIconUI), "characterState").SetValue(unitAbilityIconUI, characterState);

            // Get transform adjustments from configuration
            var transformConfig = configuration.GetSection("transform");
            if (transformConfig != null)
            {
                // Position adjustment
                var positionConfig = transformConfig.GetSection("position");
                var positionX = positionConfig.GetSection("x").ParseFloat();
                var positionY = positionConfig.GetSection("y").ParseFloat();
                var positionZ = positionConfig.GetSection("z").ParseFloat();

                if (positionX.HasValue || positionY.HasValue || positionZ.HasValue)
                {
                    var currentPos = characterUIObject.transform.localPosition;
                    characterUIObject.transform.localPosition = new Vector3(
                        positionX ?? currentPos.x,
                        positionY ?? currentPos.y,
                        positionZ ?? currentPos.z
                    );
                }

                // Scale adjustment
                var scaleX = transformConfig.GetSection("scale").GetSection("x").ParseFloat();
                var scaleY = transformConfig.GetSection("scale").GetSection("y").ParseFloat();
                var scaleZ = transformConfig.GetSection("scale").GetSection("z").ParseFloat();

                if (scaleX.HasValue || scaleY.HasValue || scaleZ.HasValue)
                {
                    var currentScale = characterUIObject.transform.localScale;
                    characterUIObject.transform.localScale = new Vector3(
                        scaleX ?? currentScale.x,
                        scaleY ?? currentScale.y,
                        scaleZ ?? currentScale.z
                    );
                }
            }
        }

        private void CopyFallbackPrefab(GameObject original, GameObject prefab)
        {
            var characterPrefab = GameObject.Instantiate(prefab);

            foreach (var component in original.GetComponents<Component>())
            {
                if (component is Transform) 
                    continue;
                GameObject.Destroy(component);
            }
            original.transform.DestroyAllChildren();
            original.layer = 0;
            int childCount = characterPrefab.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = characterPrefab.transform.GetChild(i);
                child.SetParent(original.transform);
            }
            foreach (Component component in characterPrefab.GetComponents<Component>())
            {
                if (component is Transform) 
                    continue;

                Component newComponent = original.AddComponent(component.GetType());
                System.Type componentType = component.GetType();

                foreach (var field in componentType.GetFields())
                {
                    if (field.IsLiteral)
                        continue;

                    field.SetValue(newComponent, field.GetValue(component));
                }
            }
            GameObject.Destroy(characterPrefab);
        }

        // Helper function to create Color from config section
        Color GetColorFromSection(IConfigurationSection section)
        {
            return new Color(
                section.GetSection("r").ParseFloat() ?? 1f,
                section.GetSection("g").ParseFloat() ?? 1f,
                section.GetSection("b").ParseFloat() ?? 1f,
                section.GetSection("a").ParseFloat() ?? 1f
            );
        }

        // Apply color properties if they exist on the material
        void TrySetMaterialColor(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }
    }
}