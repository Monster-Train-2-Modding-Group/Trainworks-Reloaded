using HarmonyLib;
using TrainworksReloaded.Base;
using TrainworksReloaded.Base.Class;
using TrainworksReloaded.Core;
using UnityEngine;

namespace TrainworksReloaded.Plugin.Patches
{
    // Patch that Overrides the OverrideDraftIcon.
    // Replaces the Sprite parameters if the clan has a cardDraftIcon defined.
    // This can't be injected at mod startup time as this class is not loaded at the time.
    [HarmonyPatch(typeof(RewardItemUI), "TryOverrideDraftIcon")]
    internal class ClanCardDraftIconPatch
    {
        internal static Lazy<ClassAssetsDelegator> classAssetsDelegator = new(() =>
        {
            return Railend.GetContainer().GetInstance<ClassAssetsDelegator>();
        });

        internal static Lazy<SaveManager> SaveManager = new(() =>
        {
            var client = Railend.GetContainer().GetInstance<GameDataClient>();
            if (client.TryGetProvider<SaveManager>(out var provider))
            {
                return provider;
            }
            else
            {
                return new SaveManager();
            }
        });

        static void Prefix(RewardItemUI __instance, ref Sprite mainClassIcon, ref Sprite subClassIcon)
        {
            if (__instance.rewardState?.RewardData is DraftRewardData draftRewardData)
            {
                if (draftRewardData.ClassType == RunState.ClassType.MainClass)
                {                   
                    string clan = SaveManager.Value.GetMainClass().name;
                    Sprite? icon = classAssetsDelegator.Value.GetCardDraftIcon(clan);
                    if (icon != null)
                        mainClassIcon = icon;
                }
                else if (draftRewardData.ClassType == RunState.ClassType.SubClass)
                {
                    string clan = SaveManager.Value.GetSubClass().name;
                    Sprite? icon = classAssetsDelegator.Value.GetCardDraftIcon(clan);
                    if (icon != null)
                        subClassIcon = icon;
                }
            }
        }
    }
}
