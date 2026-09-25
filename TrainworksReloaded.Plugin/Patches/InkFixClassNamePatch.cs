using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(InkStateInspector), "SetTextInsertion")]
    public class InkStateInspector_FixClassNamePatch
    {
        public static void Postfix(object[] args, Dictionary<string, string> ____insertionText)
        {
            if (args == null || args.Length < 2 || args[0].GetType() != typeof(string) || args[1].GetType() != typeof(string))
                return;
            var key = (string)args[0];
            var value = (string)args[1];
            if (key != "classname")
                return;
            var delegator = ClanCardDraftIconPatch.classAssetsDelegator.Value;

            var eventTitle = delegator.GetEventTitle(value);
            if (eventTitle == null)
                return;
            ____insertionText[key] = eventTitle.Localize();
        }
    }
}
