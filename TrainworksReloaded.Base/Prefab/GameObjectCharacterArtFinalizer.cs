using HarmonyLib;
using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{

    public class GameObjectCharacterArtFinalizer : IDataFinalizer
    {
        private readonly IModLogger<GameObjectCharacterArtFinalizer> logger;
        private readonly ICache<IDefinition<GameObject>> cache;
        private readonly FallbackDataProvider fallbackDataProvider;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly IDataFinalizer decoratee;

        public GameObjectCharacterArtFinalizer(
            IModLogger<GameObjectCharacterArtFinalizer> logger,
            ICache<IDefinition<GameObject>> cache,
            FallbackDataProvider fallbackDataProvider,
            IRegister<Sprite> spriteRegister,
            IDataFinalizer decoratee
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.fallbackDataProvider = fallbackDataProvider;
            this.spriteRegister = spriteRegister;
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

            var spriteVal = characterConfig.GetSection("sprite").ParseReference();
            if (spriteVal == null)
                return;

            var id = spriteVal.ToId(definition.Key, TemplateConstants.Sprite);
            if (!spriteRegister.TryLookupId(id, out var sprite, out _))
                return;

            var fallbackData = fallbackDataProvider.FallbackData;
            var prefab = fallbackData.GetDefaultCharacterPrefab();
            var characterPrefab = GameObject.Instantiate(prefab);

            var original = definition.Data;

            foreach (var component in original.GetComponents<Component>())
            {
                if (component is Transform) continue;
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
                if (component is Transform) continue;

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

            var character_scale = original.transform.Find("CharacterScale");
            if (character_scale == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find CharacterScale component on prefab for {definition.Key}");
                return;
            }

            var characterUIObject = character_scale.Find("CharacterUI");
            if (characterUIObject == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find CharacterUI component on CharacterScale for {definition.Key}");
                return;
            }

            var quad_default = characterUIObject.Find("Quad_Default");
            if (quad_default == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find Quad_Default component on CharacterUI for {definition.Key}");
                return;
            }

            var meshRenderer = quad_default.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find MeshRenderer component on Quad_Default for {definition.Key}");
                return;
            }

            var unitAbilityUI = original.transform.Find("DetailsUIRoot/BottomAnchor/Stats/AbilityAndTriggersGroup/UnitAbilityUI");
            if (unitAbilityUI == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find UnitAbilityUI game object on prefab for {definition.Key}");
                return;
            }

            var unitAbilityIconUI = unitAbilityUI.GetComponent<UnitAbilityIconUI>();
            if (unitAbilityIconUI == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find UnitAbilityIconUI game object on UnitAbilityUI game object for {definition.Key}");
                return;
            }

            // Get shader configuration from character_art section
            var shaderConfig = characterConfig.GetSection("shader");
            var shaderName = shaderConfig?.GetSection("name")?.Value ?? "Shader Graphs/CharacterShader2.0 Graph";

            var characterShader = Shader.Find(shaderName);
            if (characterShader == null)
            {
                logger.Log(LogLevel.Error, $"Failed to find shader {shaderName} for {definition.Key}");
                return;
            }

            var material = new Material(characterShader);

            // Helper function to create Color from config section
            Color GetColorFromSection(IConfigurationSection? section)
            {
                if (section == null) return new Color(1, 1, 1, 1);
                return new Color(
                    section.GetSection("r").ParseFloat() ?? 1f,
                    section.GetSection("g").ParseFloat() ?? 1f,
                    section.GetSection("b").ParseFloat() ?? 1f,
                    section.GetSection("a").ParseFloat() ?? 1f
                );
            }

            // Apply color properties if they exist on the material
            void TrySetMaterialColor(string propertyName, Color color)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetColor(propertyName, color);
                }
            }

            // Handle color configuration
            var colorConfig = shaderConfig?.GetSection("color");
            if (colorConfig != null)
            {
                TrySetMaterialColor("_Color", GetColorFromSection(colorConfig.GetSection("color")));
                TrySetMaterialColor("_Tint", GetColorFromSection(colorConfig.GetSection("tint")));
            }
            else
            {
                var defaultColor = new Color(1, 1, 1, 1);
                TrySetMaterialColor("_Color", defaultColor);
                TrySetMaterialColor("_Tint", defaultColor);
            }

            meshRenderer.material = material;

            // Get required components
            var spriteRenderer = characterUIObject.GetComponent<SpriteRenderer>();
            var character_state = original.GetComponent<CharacterState>();
            var characterUI = characterUIObject.GetComponent<CharacterUI>();

            // Validate required components
            if (spriteRenderer == null || character_state == null || characterUI == null)
            {
                logger.Log(LogLevel.Error, $"Missing required components on prefab for {definition.Key}");
                return;
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
            AccessTools.Field(typeof(CharacterState), "sprite").SetValue(character_state, sprite);
            AccessTools.Field(typeof(CharacterState), "charUI").SetValue(character_state, characterUI);
            AccessTools.Field(typeof(UnitAbilityIconUI), "characterState").SetValue(unitAbilityIconUI, character_state);


            // Get transform adjustments from configuration
            var transformConfig = characterConfig.GetSection("transform");
            if (transformConfig != null)
            {
                // Position adjustment
                var positionX = transformConfig.GetSection("position").GetSection("x").ParseFloat();
                var positionY = transformConfig.GetSection("position").GetSection("y").ParseFloat();
                var positionZ = transformConfig.GetSection("position").GetSection("z").ParseFloat();

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
    }
}