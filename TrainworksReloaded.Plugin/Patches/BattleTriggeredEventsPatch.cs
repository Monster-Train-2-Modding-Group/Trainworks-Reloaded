using HarmonyLib;
using TrainworksReloaded.Core;
using TrainworksReloaded.Core.Interfaces;
using static BattleTriggeredEvents;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(BattleTriggeredEvents), nameof(BattleTriggeredEvents.Init))]
    public class CustomBattleTriggeredEventsPatch
    {
        private static IRegister<CharacterTriggerData.Trigger>? triggerRegister;

        public static void Postfix(Dictionary<Team.Type, TriggerDictionaries> ___teamTriggerDictionaries)
        {
            // Guaranteed to be called after Trainworks initializes.
            triggerRegister ??= Railend.GetContainer().GetInstance<IRegister<CharacterTriggerData.Trigger>>();
            Array team_types = Enum.GetValues(typeof(Team.Type));

            foreach (Team.Type item in team_types)
            {
                var triggerDictionaries = ___teamTriggerDictionaries[item];
                foreach (var trigger in triggerRegister.Values)
                {
                    if (triggerDictionaries.triggerDictionary.ContainsKey(trigger))
                        continue;
                    triggerDictionaries.triggerDictionary[trigger] = new BattleTriggers();
                }
            }
        }
    }
}
