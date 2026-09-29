using HarmonyLib;
using Microsoft.Extensions.Configuration;
using ShinyShoe;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using static CharacterUI;

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
        private const float GroundHeightMultiplier = 0.647f;

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

            Vector2 visualDimensions = characterConfig.GetSection("visual_dimensions").ParseVec2();
            float bottomPadding = characterConfig.GetSection("bottom_padding").ParseFloat() ?? 0f;

            // Get FrameAnimation data.
            AnimatedSpriteContainer? frameAnimation = null;
            var baseAnimationsConfig = characterConfig.GetSection("animations");
            if (baseAnimationsConfig.Exists())
            {
                frameAnimation = ScriptableObject.CreateInstance<AnimatedSpriteContainer>();
                frameAnimation.name = "AnimatedSpriteContainer";
                var baseModel = ScriptableObject.CreateInstance<AnimatedSpriteModel>();
                baseModel.name = "Base Model";
                baseModel.modelId = 0;
                frameAnimation.AddModel(baseModel);
                ParseNonSpineAnimation(definition, baseAnimationsConfig, null, baseModel);
                foreach (var modelConfig in characterConfig.GetSection("model_variations").GetChildren())
                {
                    string? name = modelConfig.GetSection("name").ParseString();
                    int? modelId = modelConfig.GetSection("model_id").ParseInt();
                    if (name == null || modelId == null)
                    {
                        logger.Log(LogLevel.Warning, $"Skipping {definition.Key} {definition.Id} model: {modelConfig.Path}. Missing required name, model_id.");
                        continue;
                    }    
                    var model = ScriptableObject.CreateInstance<AnimatedSpriteModel>();
                    model.name = name;
                    model.modelId = modelId.Value;
                    ParseNonSpineAnimation(definition, modelConfig.GetSection("animations"), baseModel, model);
                    frameAnimation.AddModel(model);
                }
            }
            bool usingNonSpineAnimations = frameAnimation != null;

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
            bool usingSpineAnimations = animations.Count > 0;

            // Clone the Fallback Character fields onto our GameObject.
            GameObject original = definition.Data;
            var fallbackData = fallbackDataProvider.FallbackData;
            var prefab = fallbackData.GetDefaultCharacterPrefab();
            original.CopyPrefabToObject(prefab);

            if (usingSpineAnimations)
            {
                CreateCharacterWithSkeletonAnimations(original, definition.Id, sprite, animations, characterConfig);
            }
            else if (usingNonSpineAnimations)
            {
                CreateCharacterWithFrameAnimations(original, definition.Id, sprite, frameAnimation!, characterConfig);
            }
            else
            {
                CreateCharacterWithStaticSprite(original, definition.Id, sprite, characterConfig);
            }
            PostCharacterAdjustments(original, sprite, characterConfig, usingSpineAnimations, usingNonSpineAnimations, visualDimensions, bottomPadding);
        }

        private void ParseNonSpineAnimation(IDefinition<GameObject> definition, IConfigurationSection configuration, AnimatedSpriteModel? baseModel, AnimatedSpriteModel addToModel)
        {
            foreach (var animationConfig in configuration.GetChildren())
            {
                var anim = animationConfig.GetSection("animation").ParseAnim();
                if (anim == null)
                {
                    logger.Log(LogLevel.Warning, $"Skipping {definition.Key} {definition.Id}. No animation type specified");
                    continue;
                }

                if (baseModel != null)
                {
                    var reuse = animationConfig.GetSection("reuse_base").ParseBool() ?? false;
                    if (reuse)
                    {
                        addToModel.AddClip(baseModel.GetClip(anim.Value));
                        continue;
                    }
                }

                var frameReferences = animationConfig.GetSection("frames").ParseReferences();
                var frameRate = animationConfig.GetSection("framerate").ParseFloat() ?? 12f;
                var adjustment = animationConfig.GetSection("adjustment").ParseVec2();
                var isLooping = animationConfig.GetSection("loop").ParseBool() ?? (anim == CharacterUI.Anim.Idle || anim == CharacterUI.Anim.Idle_Relentless);
                if (frameReferences.IsNullOrEmpty())
                {
                    logger.Log(LogLevel.Warning, $"Skipping {definition.Key} {definition.Id}  animation: {anim}. No frames found.");
                    continue;
                }
                List<Sprite> sprites = [];
                foreach (var frame in frameReferences)
                {
                    if (!spriteRegister.TryLookupName(frame!.ToId(definition.Key, TemplateConstants.Sprite), out var lookup, out var _, frame.context))
                        continue;
                    sprites.Add(lookup);
                }
                SpriteAnimationClip clip = ScriptableObject.CreateInstance<SpriteAnimationClip>();

                clip.animType = anim.Value;
                clip.name = $"{clip.animType} frames {sprites.Count}";
                clip.frames = sprites;
                clip.frameRate = frameRate;
                clip.adjustment = adjustment;
                clip.isLooping = isLooping;

                if (clip.animType == CharacterUI.Anim.Attack && clip.TotalDuration >= 0.50f)
                {
                    logger.Log(LogLevel.Warning, $"Attack animation for {definition.Key} {definition.Id} exceeds 0.5s is {clip.TotalDuration}s. The animation may not fully play. Please increase framerate or shorten frames.");
                }
                if (clip.animType == CharacterUI.Anim.Death && clip.TotalDuration >= 0.70f)
                {
                    logger.Log(LogLevel.Warning, $"Death animation for {definition.Key} {definition.Id} exceeds 0.7s is {clip.TotalDuration}s. The animation may not fully play. Please increase framerate or shorten frames.");
                }
                addToModel.AddClip(clip);
            }
        }

        private void CreateCharacterWithStaticSprite(GameObject original, string name, Sprite sprite, IConfiguration configuration)
        {
            var characterUI = original.transform.Find("CharacterScale/CharacterUI");
            var spriteRenderer = characterUI?.GetComponent<SpriteRenderer>();
            var quadDefault = characterUI?.Find("Quad_Default");
            var meshRenderer = quadDefault?.GetComponent<MeshRenderer>();
            var characterUIMesh = quadDefault?.GetComponent<CharacterUIMesh>();
            var spineMeshesObject = characterUI?.Find("SpineMeshes");
            var characterUIMeshSpine = spineMeshesObject?.GetComponent<CharacterUIMeshSpine>();

            // Validate required components
            if (spriteRenderer == null || meshRenderer == null || characterUIMesh == null || characterUIMeshSpine == null || spineMeshesObject == null || quadDefault == null || characterUI == null)
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
            var shaderName = shaderConfig.GetSection("name").Value ?? "Shiny Shoe/Character Shader";
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

        private void CreateCharacterWithFrameAnimations(GameObject original, string name, Sprite sprite, AnimatedSpriteContainer frameAnimation, IConfigurationSection configuration)
        {
            var characterUI = original.transform.Find("CharacterScale/CharacterUI");
            var spriteRenderer = characterUI?.GetComponent<SpriteRenderer>();
            var quadDefault = characterUI?.Find("Quad_Default");
            var meshRenderer = quadDefault?.GetComponent<MeshRenderer>();
            var characterUIMeshOld = quadDefault?.GetComponent<CharacterUIMesh>();
            var spineMeshesObject = characterUI?.Find("SpineMeshes");
            var characterUIMeshSpine = spineMeshesObject?.GetComponent<CharacterUIMeshSpine>();

            // Validate required components
            if (spriteRenderer == null || meshRenderer == null || characterUIMeshOld == null || characterUIMeshSpine == null || spineMeshesObject == null || quadDefault == null || characterUI == null)
            {
                logger.Log(LogLevel.Error, $"Missing required components on prefab for {name}");
                return;
            }

            // Destroy and deactivate the CharacterUIMeshSpine as its not needed.
            GameObject.Destroy(characterUIMeshSpine);
            GameObject.Destroy(characterUIMeshOld);
            spineMeshesObject.gameObject.SetActive(false);

            var characterUIMesh = quadDefault.gameObject.AddComponent<CharacterUIMeshAnimatedSprite>();
            quadDefault.gameObject.SetActive(true);
            characterUIMesh.InitializeClips(frameAnimation);

            characterUIMesh.Setup(sprite, -1f, name, out var _);

            // Setup mesh renderer.
            var shaderConfig = configuration.GetSection("shader");
            var shaderName = shaderConfig.GetSection("name").Value ?? "Shader Graphs/CharacterShader2.0 Graph";
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
            dstmat.renderQueue = srcmat.renderQueue;
        }

        public static void PrintMaterialProperties(Material mat)
        {
            if (mat == null)
            {
                Debug.Log("Material is null");
                return;
            }

            // Note only prints properties that the shader expects.
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
            var characterUI = original.transform.Find("CharacterScale/CharacterUI");
            var spriteRenderer = characterUI?.GetComponent<SpriteRenderer>();
            var quadDefault = characterUI?.Find("Quad_Default");
            var meshRenderer = quadDefault?.GetComponent<MeshRenderer>();
            var spineMeshesObject = characterUI?.Find("SpineMeshes");
            var characterUIMeshSpine = spineMeshesObject?.GetComponent<CharacterUIMeshSpine>();

            // Validate required components
            if (spriteRenderer == null || quadDefault == null || characterUIMeshSpine == null || spineMeshesObject == null || characterUI == null)
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
                // Required to fix lighting issues. Animations are not affected by lighting in scene otherwise.
                animation.addNormals = true;
                animation.calculateTangents = true;
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
            quadDefault.gameObject.SetActive(false);
            characterUIMeshSpine.gameObject.SetActive(true);
            characterUIMeshSpine.Setup(sprite, -1f, name, out var _);
        }

        private static void CalculateSpineAlignment(
            Sprite sprite,
            float lowestY,
            float scaleY,
            out float uiLocalY,
            out float spineLocalY)
        {
            float extentsY = sprite != null ? sprite.bounds.extents.y : 1.5f;

            // 1. Calculate parent elevation to center the 2D silhouette
            float baseUIY = Mathf.Max(0.05f, (0.84f * extentsY) - 0.37f);

            // 2. Determine base ground drop down to the train floor plane (-0.37f)
            const float floorPlaneY = -0.45f;
            float baseDrop = baseUIY - floorPlaneY;

            // 3. Calculate unscaled (1x) Spine baseline offset
            float baseSpineY;
            if (lowestY < -0.40f && lowestY < 1000f)
            {
                // Cancel the extra distance already dipping below the root bone
                float bakedExcess = lowestY - (-0.25f);
                baseSpineY = -baseDrop - bakedExcess;
            }
            else
            {
                baseSpineY = -baseDrop;
            }

            // 4. CharacterUI - uiLocalY stays fixed at 1x baseline
            uiLocalY = baseUIY;

            // 5. Compensate SpineMeshes for vertices expanding below the root bone
            // If lowestY < 0, the feet sit |lowestY| below (0,0). When SpineMeshes scales,
            // those vertices expand down into the floor by (scaleY - 1) * |lowestY|.
            // Lifting SpineMeshes by that exact amount keeps the feet locked to the floor.
            float footDip = (lowestY < 0f && lowestY < 1000f) ? Mathf.Abs(lowestY) : 0f;
            spineLocalY = baseSpineY + (scaleY - 1f) * footDip;
        }

        public static float CalcSkeletonLowestY(SkeletonAnimation skeletonAnim)
        {
            float lowestY = float.MaxValue;
            if (skeletonAnim.Skeleton != null)
            {
                // Force a pose update to resolve the setup pose / first frame
                skeletonAnim.Skeleton.SetToSetupPose();
                skeletonAnim.Skeleton.UpdateWorldTransform(Spine.Skeleton.Physics.Reset);

                // Get the actual lowest point of all active attachment meshes
                float[] vertexBuffer = new float[8];

                foreach (var slot in skeletonAnim.Skeleton.Slots)
                {
                    if (slot.Attachment is Spine.PointAttachment || slot.Attachment == null) continue;
                    if (slot.Attachment is Spine.MeshAttachment mesh)
                    {
                        float[] worldVertices = new float[mesh.WorldVerticesLength];
                        mesh.ComputeWorldVertices(slot, 0, mesh.WorldVerticesLength, worldVertices, 0, 2);
                        for (int i = 1; i < worldVertices.Length; i += 2)
                        {
                            if (worldVertices[i] < lowestY) lowestY = worldVertices[i];
                        }
                    }
                }
            }

            return lowestY;
        }

        private void GetVisualUnits(Sprite sprite, float bottomPadding, Vector2 visualBounds, out float bottomUnits, out float heightUnits, out float halfWidthUnits)
        {
            float ppu = sprite != null ? sprite.pixelsPerUnit : 100f;

            // If not specified or <= 0 in JSON, fallback to raw canvas size
            heightUnits = visualBounds.y > 0f
                ? (visualBounds.y / ppu)
                : (sprite != null ? sprite.bounds.size.y : 3.0f);

            halfWidthUnits = visualBounds.x > 0f
                ? ((visualBounds.x * 0.5f) / ppu)
                : (sprite != null ? sprite.bounds.extents.x : 1.5f);

            bottomUnits = bottomPadding / ppu;
        }

        private void SetupVfxAnchors(
            CharacterUI charUI,
            bool isSpine,
            float scaleFactorX,
            float scaleFactorY,
            float extentsX,
            float extentsY,
            float lowestY = 0f,
            float spineLocalY = 0f,
            float bottomPadding = 0f,
            Vector2? visualBounds = null)
        {
            Transform vfxContainer = charUI.transform.Find("VfxAnchors");
            if (vfxContainer == null) return;

            // 1. Keep the container strictly unscaled at (0, 0, 0)
            vfxContainer.localPosition = Vector3.zero;
            vfxContainer.localScale = Vector3.one;

            float bottomY;
            float topY;
            float centerY;
            float sideX = Mathf.Max(1.44f, extentsX * scaleFactorX);

            if (isSpine)
            {
                // -------------------------------------------------------------
                // Spine: Scales outward from (0, spineLocalY)
                // -------------------------------------------------------------
                float validLowestY = (lowestY < 1000f) ? lowestY : 0f;
                float totalHeight = extentsY * 2f;

                // Feet position in CharacterUI space
                bottomY = spineLocalY + (validLowestY * scaleFactorY) + 0.15f;

                // Head position in CharacterUI space
                topY = spineLocalY + ((validLowestY + totalHeight) * scaleFactorY);

                centerY = (bottomY + topY) * 0.5f;
            }
            else
            {
                // -------------------------------------------------------------
                // [Animated] Sprite: Scales outward symmetrically from quad center
                // -------------------------------------------------------------
                Sprite sprite = charUI.GetComponent<SpriteRenderer>().sprite;
                GetVisualUnits(sprite, bottomPadding, visualBounds ?? Vector2.zero, out var bottomUnits, out var heightUnits, out var halfWidthUnits);

                // Unscaled (1x) base offsets relative to quad center (0, 0)
                float baseBottomY = -extentsY + bottomUnits;
                float baseTopY = baseBottomY + heightUnits;
                float baseCenterY = baseBottomY + (heightUnits * 0.5f);
                float baseSideX = Mathf.Max(1.44f, halfWidthUnits);

                // Feet at quad bottom edge
                bottomY = baseBottomY * scaleFactorY;

                // Head at quad top edge (+ 0.15f overhead buffer for status icons)
                topY = baseTopY * scaleFactorY + 0.15f;

                centerY = baseCenterY * scaleFactorY;
                sideX = baseSideX * scaleFactorX;
            }

            void SetPos(string name, float x, float y)
            {
                Transform t = vfxContainer.Find(name);
                t?.localPosition = new Vector3(x, y, 0f);
            }

            // Ground anchors
            SetPos("VfxBottomAnchor", 0f, bottomY);
            SetPos("VfxSideFwdBottomAnchor", 0f, 0f);
            SetPos("VfxSideBackBottomAnchor", 0f, 0f);

            // Torso / Center line
            SetPos("VfxCenterAnchor", 0f, centerY);
            SetPos("VfxSideFwdAnchor", sideX, centerY);
            SetPos("VfxSideBackAnchor", -sideX, centerY);

            // Overhead anchor
            SetPos("VfxTopAnchor", 0f, topY);
        }
        private void PostCharacterAdjustments(GameObject original, Sprite sprite, IConfiguration configuration, bool usingSpineAnimations, bool usingNonSpineAnimations, Vector2 visualDimensions, float bottomPadding)
        {
            var characterState = original.GetComponent<CharacterState>();
            var characterUITransform = original.transform.Find("CharacterScale/CharacterUI");
            var characterUI = original.transform.Find("CharacterScale/CharacterUI")?.GetComponent<CharacterUI>();
            var unitAbilityIconUI = original.transform.Find("DetailsUIRoot/BottomAnchor/Stats/AbilityAndTriggersGroup/UnitAbilityUI")?.GetComponent<UnitAbilityIconUI>();
            var spineMeshesTransform = characterUITransform.Find("SpineMeshes");
            var quadDefaultTransform = characterUITransform.transform.Find("Quad_Default");

            // Bypass calling CharacterState.InitialSetup
            AccessTools.Field(typeof(CharacterState), "sprite").SetValue(characterState, sprite);
            AccessTools.Field(typeof(CharacterState), "charUI").SetValue(characterState, characterUI);
            AccessTools.Field(typeof(UnitAbilityIconUI), "characterState").SetValue(unitAbilityIconUI, characterState);

            if (!usingSpineAnimations /*&& !usingNonSpineAnimations*/)
            {
                var layer = configuration.GetSection("layer").ParseInt() ?? 20;
                characterUITransform.gameObject.layer = layer;
                quadDefaultTransform.gameObject.layer = layer;
            }

            // Get transform adjustments from configuration
            var transformConfig = configuration.GetSection("transform");

            // Scale adjustment
            var scaleConfig = transformConfig.GetSection("scale");
            Vector3 scale = Vector3.one;
            if (scaleConfig.Exists())
            {
                var transform = usingSpineAnimations ? spineMeshesTransform : quadDefaultTransform;
                var currentScale = transform.localScale;
                scale = scaleConfig.ParseVec3(currentScale.x, currentScale.y, currentScale.z);
                if (usingNonSpineAnimations || usingSpineAnimations)
                    transform.localScale = scale;
                else
                {
                    transform.localScale = scale;
                    // Unfortunately modifying Quad_Default scale messes things up. Fix later with a patch with this GameObject.
                    var scaleFixer = new GameObject
                    {
                        name = "ScaleFixer"
                    };
                    scaleFixer.transform.parent = quadDefaultTransform;
                    scaleFixer.transform.localScale = scale;
                }
            }

            var skeletonLowestY = 0f;

            if (usingSpineAnimations)
            {
                var skeletonAnimation = spineMeshesTransform.GetChild(0)?.GetComponent<SkeletonAnimation>();
                skeletonLowestY = CalcSkeletonLowestY(skeletonAnimation!);
                CalculateSpineAlignment(sprite, skeletonLowestY, spineMeshesTransform.transform.localScale.y, out float uiLocalY, out float spineLocalY);
                characterUITransform.localPosition = new Vector3(0f, uiLocalY, 0f);
                spineMeshesTransform.localPosition = new Vector3(0f, spineLocalY, 0f);
            }
            else if (usingNonSpineAnimations)
            {
                // Calculate half-height dynamically from the sprite's actual mesh bounds
                float floorOffsetY = sprite.bounds.extents.y * GroundHeightMultiplier * quadDefaultTransform.localScale.y;
                // Elevate CharacterUI so the bottom edge sits on the floor
                characterUITransform.transform.localPosition = new Vector3(0f, floorOffsetY, 0f);
            }
            else
            {
                float floorOffsetY = sprite.bounds.extents.y * GroundHeightMultiplier * scale.y;
                // Elevate CharacterUI so the bottom edge sits on the floor
                characterUITransform.transform.localPosition = new Vector3(0f, floorOffsetY, 0f);
            }


            // Absolute positioning if requested.
            var positionConfig = transformConfig.GetSection("position");
            if (positionConfig.Exists())
            {
                var currentPosition = characterUITransform.localPosition;
                var position = positionConfig.ParseVec3(currentPosition.x, currentPosition.y, currentPosition.z);
                characterUITransform.localPosition = position;
            }

            // Position adjustment
            var offsetPositionConfig = transformConfig.GetSection("offset_position");
            if (offsetPositionConfig.Exists())
            {
                characterUITransform.localPosition += offsetPositionConfig.ParseVec3();
            }

            if (!usingSpineAnimations)
            {
                float extentsX = sprite.bounds.extents.x;
                float extentsY = sprite.bounds.extents.y;

                SetupVfxAnchors(
                    charUI: characterUI!,
                    isSpine: false,
                    scaleFactorX: scale.x,
                    scaleFactorY: scale.y,
                    extentsX: extentsX,
                    extentsY: extentsY,
                    visualBounds: visualDimensions,
                    bottomPadding: bottomPadding
                );
            }
            else
            {
                float extentsX = sprite != null ? sprite.bounds.extents.x : 1.5f;
                float extentsY = sprite != null ? sprite.bounds.extents.y : 1.5f;
                float spineY = spineMeshesTransform.localPosition.y;

                SetupVfxAnchors(
                    charUI: characterUI!,
                    isSpine: true,
                    scaleFactorX: scale.x,
                    scaleFactorY: scale.y,
                    extentsX: extentsX,
                    extentsY: extentsY,
                    lowestY: skeletonLowestY,
                    spineLocalY: spineY
                );
            }

            AccessTools.Field(typeof(CharacterUI), "usePrefabPlacementOfSideFwdAnchor").SetValue(characterUI!, true);
            AccessTools.Field(typeof(CharacterUI), "usePrefabPlacementOfSideBackAnchor").SetValue(characterUI!, true);

            if (!usingNonSpineAnimations && !usingSpineAnimations)
            {
                // Set the scale directly since it will be blocked in Setup.
                var localScale = quadDefaultTransform.localScale;
                //quadDefaultTransform.localScale = new Vector3(sprite!.bounds.size.x * localScale.x, sprite.bounds.size.y * localScale.y, localScale.z);
            }
        }

        // Helper function to create Color from config section
        Color GetColorFromSection(IConfigurationSection section)
        {
            return section.ParseColor() ?? Color.white;
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