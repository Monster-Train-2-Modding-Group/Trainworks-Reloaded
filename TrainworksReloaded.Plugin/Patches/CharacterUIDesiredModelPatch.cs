using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Base.Prefab;

namespace TrainworksReloaded.Plugin.Patches
{
    // Patch blocks calling this function as its not going to be defined for Animated Sprites
    // This is mainly geared for FunGuy support which won't exist and will always reset the model.
    [HarmonyPatch(typeof(CharacterUI), "SetDesiredModelIndex")]
    public class CharacterUIDesiredModelPatch
    {
        
        public static bool Prefix(CharacterUI __instance)
        {
            var component = __instance.transform.Find("Quad_Default")?.GetComponent<CharacterUIMeshAnimatedSprite>();
            if (component != null)
            {
                return false;
            }
            return true;
        }
    }
}
