using HarmonyLib;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Card
{
    public class CardPoolRegister : Dictionary<string, CardPool>, IRegister<CardPool>
    {
        private readonly IModLogger<CardPoolRegister> logger;
        private readonly Dictionary<string, CardPool> VanillaCardPools = [];
        // Map of ClassName to BannerReplacementMutatorPool (Just draftable cards from MegaPool for class)
        private readonly Dictionary<string, CardPool?> ClassDraftableCardPools = [];

        public CardPoolRegister(IModLogger<CardPoolRegister> logger)
        {
            this.logger = logger;
            VanillaCardPools.AddRange(Resources.FindObjectsOfTypeAll<CardPool>().ToDictionary(x => x.name, x => x));
            VanillaCardPools.Remove("ModdedPool");
            this.AddRange(VanillaCardPools);
            FormVanillaClassDraftableCardPools();
        }

        private void FormVanillaClassDraftableCardPools()
        {
            ClassDraftableCardPools.Add("ClassBanished", VanillaCardPools.GetValueOrDefault("UnitsBanishedBannerReplacementMutator"));
            ClassDraftableCardPools.Add("ClassPyreborne", VanillaCardPools.GetValueOrDefault("UnitsPyreborneBannerReplacementMutator"));
            ClassDraftableCardPools.Add("ClassLunaCoven", VanillaCardPools.GetValueOrDefault("UnitsLunaCovenBannerReplacementMutator"));
            ClassDraftableCardPools.Add("ClassUnderlegion", VanillaCardPools.GetValueOrDefault("UnitsUnderlegionBannerReplacementMutator"));
            ClassDraftableCardPools.Add("ClassLazarusLeague", VanillaCardPools.GetValueOrDefault("UnitsLazarusLeagueBannerReplacementMutator"));
            
            ClassDraftableCardPools.Add("ClassHellhorned", VanillaCardPools.GetValueOrDefault("UnitsHellhornedBannerReplacementMutator"));
            ClassDraftableCardPools.Add("ClassAwoken", VanillaCardPools.GetValueOrDefault("UnitsAwokenBannerReplacementMutator"));
            ClassDraftableCardPools.Add("ClassStygian", VanillaCardPools.GetValueOrDefault("UnitsStygianBannerReplacementMutator"));
            ClassDraftableCardPools.Add("ClassUmbra", VanillaCardPools.GetValueOrDefault("UnitsUmbraBannerReplacementMutator"));

            // TODO Currently there is a bug w/ base game that these card pools don't exist.
            //ClassDraftableCardPools.Add("ClassRailforged", VanillaCardPools.GetValueOrDefault());
            //ClassDraftableCardPools.Add("ClassRemnant", VanillaCardPools.GetValueOrDefault());
            //ClassDraftableCardPools.Add("ClassWurm", VanillaCardPools.GetValueOrDefault());
        }

        public void Register(string key, CardPool item)
        {
            logger.Log(LogLevel.Info, $"Register Card Pool {key}...");
            Add(key, item);
        }

        public List<string> GetAllIdentifiers(RegisterIdentifierType identifierType)
        {
            return [.. this.Keys];
        }

        public bool TryLookupIdentifier(
            string identifier,
            RegisterIdentifierType identifierType,
            [NotNullWhen(true)] out CardPool? lookup,
            [NotNullWhen(true)] out bool? IsModded
        )
        {
            IsModded = !VanillaCardPools.ContainsKey(identifier);
            return this.TryGetValue(identifier, out lookup);
        }

        internal void RegisterBannerReplacementPool(string classname, CardPool replacementCardPool)
        {
             ClassDraftableCardPools[classname] = replacementCardPool;
        }

        public CardPool? GetBannerReplacementPool(string classname)
        {
            return ClassDraftableCardPools.GetValueOrDefault(classname);
        }
    }
}
