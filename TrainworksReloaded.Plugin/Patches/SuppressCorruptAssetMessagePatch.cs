using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(AppDialogNotifications), "HandleCorruptAssetSignal")]
    public class SuppressCorruptAssetMessagePatch
    {
        public static bool seen = false;
        public static bool Prefix()
        {
            if (!seen)
            {
                Plugin.Logger.LogInfo("Suppressing Corrupt Asset Dialog, if you aren't testing cards then you should verify integrity of your game files and/or if this is occuring with official content report a bug");
                seen = true;
            }
            return false;
        }
    }
}
