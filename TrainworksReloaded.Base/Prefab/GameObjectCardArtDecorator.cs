using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace TrainworksReloaded.Base.Prefab
{
    public class GameObjectCardArtDecorator : IDataPipeline<IRegister<GameObject>, GameObject>
    {
        private readonly IDataPipeline<IRegister<GameObject>, GameObject> decoratee;
        private readonly IRegister<Sprite> spriteRegister;

        public GameObjectCardArtDecorator(
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
            if (type != "card_art")
                return;

            var spriteVal = definition
                .Configuration.GetSection("extensions")
                .GetSection("card_art")
                .GetSection("sprite")
                .ParseReference();
            if (spriteVal == null)
                return;

            var id = spriteVal.ToId(definition.Key, TemplateConstants.Sprite);
            if (!spriteRegister.TryLookupId(id, out var sprite, out _))
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
