using HarmonyLib;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(CharacterUIMesh), nameof(CharacterUIMesh.Setup))]
    public class CharacterUIMeshScalePatch
    {
        // Patch scales up QuadDefault by the requested scale from construction afterwards.
        // Patch overrides the scaling set by the Setup function which sets it to the size of the sprite.
        static void Postfix(CharacterUIMesh __instance)
        {
            var scaleFixer = __instance.transform.Find("ScaleFixer");
            if (scaleFixer != null)
            {
                Vector3 scale = scaleFixer.localScale;
                Vector3 size = __instance.transform.localScale;
                __instance.meshRenderer.transform.localScale = new Vector3(scale.x * size.x, scale.y * size.y, scale.z * size.z);
            }
            
        }
    }
}
