using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Base.Tooltips;
using static TooltipDesigner;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(TooltipDesigner), nameof(TooltipDesigner.GetTooltipDesignData))]
    public class TooltipDesignerPatch
    {
        internal static TooltipDesignDataDelegator? delegator;
        public static bool Prefix(TooltipDesignType tooltipDesignType, ref TooltipDesignData __result)
        {
            var data = delegator?.Get(tooltipDesignType);
            if (data != null)
            {
                __result = data;
                return false; // skip
            }
            return true;
        }
    }
}
