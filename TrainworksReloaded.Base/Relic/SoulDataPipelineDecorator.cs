using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Relic
{
    public class SoulDataPipelineDecorator : IDataPipeline<IRegister<RelicData>, RelicData>
    {
        private readonly IModLogger<SoulDataPipelineDecorator> logger;
        private readonly IDataPipeline<IRegister<RelicData>, RelicData> decoratee;
        private readonly IRegister<LocalizationTerm> localizationRegister;

        FieldInfo DraftMinDistanceAllowedField = AccessTools.Field(typeof(SoulData), "draftMinDistanceAllowed");
        FieldInfo DraftMaxDistanceAllowedField = AccessTools.Field(typeof(SoulData), "draftMaxDistanceAllowed");
        FieldInfo DraftMinBattlesCompletedField = AccessTools.Field(typeof(SoulData), "draftMinBattlesCompleted");
        FieldInfo DraftMaxBattlesCompletedField = AccessTools.Field(typeof(SoulData), "draftMaxBattlesCompleted");

        public SoulDataPipelineDecorator(
            IModLogger<SoulDataPipelineDecorator> logger,
            IRegister<LocalizationTerm> localizationRegister,
            IDataPipeline<IRegister<RelicData>, RelicData> decoratee
        )
        {
            this.logger = logger;
            this.localizationRegister = localizationRegister;
            this.decoratee = decoratee;
        }

        public List<IDefinition<RelicData>> Run(IRegister<RelicData> register)
        {
            var definitions = decoratee.Run(register);
            foreach (var definition in definitions)
            {
                ProcessItem((definition as RelicDataDefinition)!);
            }
            return definitions;
        }

        private void ProcessItem(RelicDataDefinition definition)
        {
            var config = definition.Configuration;
            var data = definition.Data;
            var overrideMode = definition.Override;
            var key = definition.Key;
            var relicId = definition.Id.ToId(key, TemplateConstants.RelicData);

            if (data is not SoulData soul)
                return;

            if (definition.CopyData is not SoulData copyData)
                copyData = soul;

            var configuration = config
                .GetSection("extensions")
                .GetChildren()
                .Where(xs => xs.GetSection("soul").Exists())
                .Select(xs => xs.GetSection("soul"))
                .FirstOrDefault();

            if (configuration == null)
                return;

            var tierLevel = configuration.GetSection("tier_level").ParseInt() ?? copyData.GetTierLevel();
            AccessTools.Field(typeof(SoulData), "tierLevel").SetValue(soul, tierLevel);

            var requiredDLC = configuration.GetSection("required_dlc").ParseDLC() ?? copyData.GetRequiredDLC();
            AccessTools.Field(typeof(SoulData), "requiredDLC").SetValue(soul, requiredDLC);

            var unlockLevel = configuration.GetSection("unlock_level").ParseInt() ?? copyData.GetUnlockLevel();
            AccessTools.Field(typeof(SoulData), "unlockLevel").SetValue(soul, unlockLevel);

            var unlockCriteriaConfig = configuration.GetSection("unlock_criteria");
            var copyUnlockConfig = copyData.GetUnlockCriteria();

            if (unlockCriteriaConfig.Exists())
            {
                var unlockCriteria = soul.GetUnlockCriteria();
                var term = unlockCriteriaConfig.GetSection("descriptions").ParseLocalizationTerm();
                var termKey = copyUnlockConfig.GetDescriptionKey();
                if (term != null)
                {
                    termKey = $"SoulData_descriptionKey-{soul.GetAssetKey()}";
                    
                    term.Key = termKey;
                    localizationRegister.Register(termKey, term);
                }
                AccessTools.Field(typeof(UnlockCriteria), "descriptionKey").SetValue(unlockCriteria, termKey);

                var condition = unlockCriteriaConfig.GetSection("unlock_condition").ParseTrackedValue() ?? copyUnlockConfig.GetUnlockCondition();
                AccessTools.Field(typeof(UnlockCriteria), "unlockCondition").SetValue(unlockCriteria, condition);

                var paramInt = unlockCriteriaConfig.GetSection("param_int").ParseInt() ?? copyUnlockConfig.GetParamInt();
                AccessTools.Field(typeof(UnlockCriteria), "paramInt").SetValue(unlockCriteria, paramInt);
            }
            else
            {
                AccessTools.Field(typeof(SoulData), "unlockData").SetValue(soul, copyUnlockConfig);
            }

            var draftMinDistanceAllowed = configuration.GetSection("draft_min_distance_allowed").ParseInt() ?? DraftMinDistanceAllowedField.GetValue(copyData);
            DraftMinDistanceAllowedField.SetValue(soul, draftMinDistanceAllowed);

            var draftMaxDistanceAllowed = configuration.GetSection("draft_max_distance_allowed").ParseInt() ?? DraftMaxDistanceAllowedField.GetValue(copyData);
            DraftMaxDistanceAllowedField.SetValue(soul, draftMaxDistanceAllowed);

            var draftMinBattlesCompleted = configuration.GetSection("draft_min_battles_completed").ParseInt() ?? DraftMinBattlesCompletedField.GetValue(copyData);
            DraftMinBattlesCompletedField.SetValue(soul, draftMinBattlesCompleted);

            var draftMaxBattlesCompleted = configuration.GetSection("draft_max_battles_completed").ParseInt() ?? DraftMaxBattlesCompletedField.GetValue(copyData);
            DraftMaxBattlesCompletedField.SetValue(soul, draftMaxBattlesCompleted);
        }
    }
}