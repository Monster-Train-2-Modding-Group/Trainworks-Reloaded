using HarmonyLib;
using Spine;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TrainworksReloaded.Plugin.Patches
{
    /// <summary>
    /// Patch due to not being able to store binary data in a TextAsset since it will get corrupted.
    /// Instead we hijack ReadSkeletonData to recognize a base64 data with a special header, decode, then process as normal.
    /// </summary>
    [HarmonyPatch(typeof(SkeletonDataAsset), "ReadSkeletonData", [typeof(byte[]), typeof(AttachmentLoader), typeof(float)])]
    public class SkeletonDataAssetPatch
    {
        static bool Prefix(ref byte[] bytes)
        {
            string asText = Encoding.UTF8.GetString(bytes);

            const string header = "SPINE64|";
            if (asText.StartsWith(header))
            {
                bytes = Convert.FromBase64String(asText[header.Length..]);
            }
            return true; // allow original method to run with modified bytes
        }
    }
}
