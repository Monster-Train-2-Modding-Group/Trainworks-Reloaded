using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Base.Class;
using UnityEngine;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(ClassSpriteSelector), "GetSprite")]
    public class ClassCardStylePatch
    {
        internal static ClassCardStyleDelegator? delegator;

        public static bool Prefix(ref Sprite __result, CardType cardType, ClassCardStyle classCardStyle)
        {
            if (delegator == null)
            {
                return true;
            }
            var sprites = delegator.GetClassCardStyleSprites(classCardStyle);
            if (sprites == null)
            {
                return true;
            }
            var sprite = sprites.GetValueOrDefault(cardType);
            if (sprite == null)
            {
                return true;
            }
            __result = sprite;
            // skip original
            return false;

        }
    }
}
