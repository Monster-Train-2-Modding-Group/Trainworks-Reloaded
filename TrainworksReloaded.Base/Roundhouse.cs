using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Core;

namespace TrainworksReloaded.Base
{
    /// <summary>
    /// Utility class for modders to add additional initialization steps as the framework loads and processes JSON files.
    /// This class allows once to get GameData and GameObjects at these points
    /// 
    /// 1: After JSON files are read/merge but before they are processed and GameData objects are created. (WhenParsed)
    /// 2: After GameData objects are created, but before finalization happens and GameData objects are wired together (WhenCreated)
    /// 3: After all finalizers are ran and everything is ready (WhenReady)
    /// 
    /// This class should be used instead of placing HarmonyPatches on the framework, which should be avoided as refactors can change functions
    /// and their arguments and we will not be responsible for breakages that occur when a non patch solution exists.
    /// </summary>
    public static class Roundhouse
    {
        /// <summary>
        /// Configure a function to be called after JSON files are loaded, but 
        /// before the creation of any GameData objects.
        /// <see>Railend.ConfigurePreAction</see>
        /// </summary>
        /// <param name="pluginGuid">Plugin's GUID</param>
        /// <param name="action">Function</param>
        public static void WhenParsed(string pluginGuid, Action<GameDataManager> action)
        {
            Railend.ConfigurePreAction(c =>
            {
                var context = new GameDataManager(c, pluginGuid);
                action(context);
            });
        }

        /// <summary>
        /// Configure a function to be called before all Finalizers are ran.
        /// At this point the GameData objects will be created but not linked.
        /// Localization data will not be loaded at this point
        /// <see>Railend.ConfigurePostAction</see>
        /// </summary>
        /// <param name="pluginGuid">Plugin's GUID</param>
        /// <param name="action">Function</param>
        public static void WhenCreated(string pluginGuid, Action<GameDataManager> action)
        {
            Railend.ConfigurePostAction(c =>
            {
                var context = new GameDataManager(c, pluginGuid);
                action(context);
            });
        }

        /// <summary>
        /// Configure a function to be called after all of the GameData is setup and finalized.
        /// All GameData objects will be fully created, and localizations will have been
        /// uploaded to I2.Loc.
        /// 
        /// This is useful for adding additional FX to any GameObjects created by the framework.
        /// </summary>
        /// <param name="pluginGuid">Plugin's GUID</param>
        /// <param name="action">Function</param>
        public static void WhenReady(string pluginGuid, Action<GameDataManager> action)
        {
            Railend.ConfigurePostFinalizerAction(c =>
            {
                var context = new GameDataManager(c, pluginGuid);
                action(context);
            });
        }
    }
}
