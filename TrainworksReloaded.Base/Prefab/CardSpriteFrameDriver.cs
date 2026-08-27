using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static AnimateCardEffects;
using static RotaryHeart.Lib.DataBaseExample;

namespace TrainworksReloaded.Base.Prefab
{
    public class CardSpriteFrameDriver : MonoBehaviour
    {
        public Image? TargetImage;
        public Sprite[]? Frames;

        [SerializeField]
        public CardEffectTransform? FrameHolder;
        private int _lastIndex = -1;

        private void Awake()
        {
            if (TargetImage == null)
            {
                TargetImage = GetComponent<Image>();
                FrameHolder = GetComponent<CardEffectTransform>();
            }
        }

        private void LateUpdate()
        {
            if (Frames == null || Frames.Length == 0 || TargetImage == null) return;

            int index = Mathf.Clamp((int)FrameHolder!.GetPosition().x, 0, Frames.Length - 1);
            if (index != _lastIndex)
            {
                _lastIndex = index;
                TargetImage.sprite = Frames[index];
                /*var material = TargetImage.material;
                material.mainTexture = Frames[index].texture;
                material.SetTexture("_Layer1Tex", Frames[index].texture);
                material.SetFloat($"_Layer1Enabled", 1);
                material.SetFloat($"_Layer1Type", 1);
                TargetImage.SetMaterialDirty();
                TargetImage.SetVerticesDirty();
                TargetImage.canvasRenderer.SetMaterial(TargetImage.material, 0);*/
            }
        }
    }
}
