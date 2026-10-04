using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Core;

namespace TrainworksReloaded.Base
{
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
