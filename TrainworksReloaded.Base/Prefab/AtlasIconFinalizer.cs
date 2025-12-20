using System.Collections.Generic;
using TMPro;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;
using UnityEngine.TextCore;

namespace TrainworksReloaded.Base.Prefab
{
    public class AtlasIconFinalizer(
        ICache<IDefinition<Texture2D>> cache
        ) : IDataFinalizer
    {
        private readonly ICache<IDefinition<Texture2D>> cache = cache;

        public void FinalizeData()
        {
            List<Texture2D> textures = [];
            foreach (var definition in cache.GetCacheItems())
            {
                textures.Add(definition.Data);
            }
            cache.Clear();

            // This code is a copy from TMP_SpriteAsset.UpgradeSpriteAsset.
            // That function has a bug in that the SpriteCharacters created all point to the same SpriteGlyph.
            // That's due to that code failing to set the proper glphyIndex. 

            // Basic setup, Create a Sprite Atlas.
            //
            // Padding to ensure that the edges of the other images aren't picked up.
            // The base game uses a gap of 6 pixels.
            var atlas = new Texture2D(2, 2);
            var rects = atlas.PackTextures(textures.ToArray(), padding: 6);
            var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            spriteAsset.name = "Trainworks Sprite Atlas";
            spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);
            spriteAsset.spriteSheet = atlas;
            spriteAsset.spriteInfoList = [];
            spriteAsset.material = new Material(Shader.Find("TextMeshPro/Sprite"))
            {
                mainTexture = atlas
            };

            // Create the SpriteCharacters and SpriteGlyphs.
            var spriteGlyphTable = spriteAsset.spriteGlyphTable;
            var spriteCharacterTable = spriteAsset.spriteCharacterTable;
            // GlyphMetrics are constant, all icons are this size, if a texture is bigger then it will be resized to fit automatically.
            // BearingY is a hardcoded value to get the icons to align properly with the text.
            var metrics = new GlyphMetrics(width: 24, height: 24, bearingX: 0, bearingY: 20, advance: 24);
            for (int j = 0; j < rects.Length; j++)
            {
                var texture = textures[j];
                var rect = rects[j];
                int x = (int)(rect.x * atlas.width);
                int y = (int)(rect.y * atlas.height);
                var sprite = Sprite.Create(atlas, new Rect(x, y, texture.width, texture.height), new Vector2(0.5f, 0.5f), 128f);
                sprite.name = texture.name;
                TMP_SpriteGlyph tMP_SpriteGlyph = new()
                {
                    index = (uint)j,
                    sprite = sprite,
                    metrics = metrics,
                    glyphRect = new GlyphRect(x, y, texture.width, texture.height),
                    scale = 1f,
                    atlasIndex = 0
                };
                spriteGlyphTable.Add(tMP_SpriteGlyph);
                TMP_SpriteCharacter tMP_SpriteCharacter = new()
                {
                    glyph = tMP_SpriteGlyph,
                    glyphIndex = (uint)j,
                    unicode = 65534u, // special constant for sprites.
                    name = sprite.name,
                    scale = 1
                };
                spriteCharacterTable.Add(tMP_SpriteCharacter);
            }

            spriteAsset.UpdateLookupTables();
            TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets.Add(spriteAsset);
        }
    }
}
