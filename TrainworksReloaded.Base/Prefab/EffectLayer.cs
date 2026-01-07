using System;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public struct EffectLayer
    {
        public CardEffectsMaterial.EffectType type;
        public Texture2D? texture;
        public Texture2D? motion_texture;
        public Texture2D? mask_texture;
        public Color tint = Color.white;
        public Vector2 linear_offset = Vector2.zero;
        public Vector2 linear_speed = Vector2.zero;
        public Vector3 tilt = Vector3.zero;
        public Vector3 rotation_speed = Vector3.zero;
        public Vector2 scale = Vector2.one;
        public Vector2 position_offset = Vector2.zero;
        public bool stretch = true;
        public bool additive = true;
        public bool enabled = true;

        public EffectLayer()
        {
        }
    }

    public struct EffectTransform
    {
        public string name = string.Empty;
        public int[] layers = Array.Empty<int>();
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.one;

        public EffectTransform()
        {
        }
    }
}
