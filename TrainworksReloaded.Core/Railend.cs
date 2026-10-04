using BepInEx.Logging;
using SimpleInjector;

namespace TrainworksReloaded.Core
{
    /// <summary>
    /// Railend is a class used by the final loader mod to load
    /// </summary>
    public class Railend
    {
        private static readonly List<Action<Container>> PreContainerActions = [];
        private static readonly List<Action<Container>> PostContainerActions = [];
        private static readonly List<Action<Container>> PostFinalizerActions = [];
        private static readonly ManualLogSource logger = Logger.CreateLogSource("Railend");
        private static readonly Lazy<Container> container = new(() =>
        {
            var init = Railhead.GetBuilderForInit();
            var container = new Container();
            foreach (var action in PreContainerActions)
            {
                try
                {
                    action(container);
                }
                catch (Exception ex)
                {
                    logger.LogError($"============================================================");
                    logger.LogError($"[CATASTROPHIC] Mod at {action.Method.DeclaringType.Assembly.GetName()} failed to load due to exception.");
                    logger.LogError($"============================================================");
                    logger.LogError(ex.ToString());
                }
            }
            init.Build(container);
            foreach (var action in PostContainerActions)
            {
                try
                {
                    action(container);
                }
                catch (Exception ex)
                {
                    logger.LogError($"============================================================");
                    logger.LogError($"[CATASTROPHIC] Mod at {action.Method.DeclaringType.Assembly.GetName()} failed to load due to exception.");
                    logger.LogError($"============================================================");
                    logger.LogError(ex.ToString());
                }
            }
            container.Verify();
            return container;
        });

        /// <summary>
        /// Registers an Action that runs on the container after the Atlas has been registered
        /// </summary>
        /// <param name="action"></param>
        public static void ConfigurePreAction(Action<Container> action)
        {
            PreContainerActions.Add(action);
        }

        /// <summary>
        /// Registers an Action that runs on the container the configuration is built.
        /// Note that GameData has not been finalized at this point so fields that reference
        /// other GameData types will not be populated at this point and localizations will not
        /// be uploaded to I2.Loc.
        /// </summary>
        /// <param name="action"></param>
        public static void ConfigurePostAction(Action<Container> action)
        {
            PostContainerActions.Add(action);
        }

        /// <summary>
        /// Registers an action that runs on the container when all of the data is setup
        /// and wired.
        /// </summary>
        /// <param name="action"></param>
        public static void ConfigurePostFinalizerAction(Action<Container> action)
        {
            PostFinalizerActions.Add(action);
        }

        /// <summary>
        /// Do not run this function, it begins the initialization process.
        /// YOU WILL BREAK COMPATIBILITY.
        /// Let Trainworks run this!
        /// 
        /// If you need the container instance. Use Railend.ConfigurePostAction
        /// and you will get passed the container instance which you can save at that point.
        /// </summary>
        public static Container GetContainer()
        {
            return container.Value;
        }

        public static IReadOnlyList<Action<Container>> GetPostFinalizerActions()
        {
            return PostFinalizerActions;
        }
    }
}
