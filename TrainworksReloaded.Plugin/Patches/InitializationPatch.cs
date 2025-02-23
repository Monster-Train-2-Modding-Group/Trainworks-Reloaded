﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Base.Card;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(AssetLoadingManager), "Start")]
    public class InitializationPatch
    {
        public static void Postfix(AssetLoadingData ____assetLoadingData)
        {
            var register = Railend.GetContainer().GetInstance<CardDataRegister>();
            ____assetLoadingData.CardPoolsAll.Add(register.CustomCardPool);

            var localization = Railend.GetContainer().GetInstance<CustomLocalizationTermRegistry>();
            localization.LoadData();
        }
    }
}
