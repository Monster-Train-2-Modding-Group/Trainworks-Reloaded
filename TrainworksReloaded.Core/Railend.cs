using SimpleInjector;

namespace TrainworksReloaded.Core
{
    /// <summary>
    /// Railend is a class used by the final loader mod to load
    /// </summary>
    public class Railend
    {
        private static readonly List<Action<Container>> PreContainerActions = new();
        private static readonly List<Action<Container>> PostContainerActions = new();
        private static readonly Lazy<Container> container = new(() =>
        {
            var init = Railhead.GetBuilderForInit();
            var container = new Container();
            foreach (var action in PreContainerActions)
            {
                action(container);
            }
            init.Build(container);
            foreach (var action in PostContainerActions)
            {
                action(container);
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

        public static void ConfigurePostAction(Action<Container> action)
        {
            PostContainerActions.Add(action);
        }

        /// <summary>
        /// Do not run this function, it begins the initialization process.
        /// YOU WILL BREAK COMPATIBILITY.
        /// Let Trainworks run this!
        /// 
        /// If you need the container instance. Use Railend.ConfigurePostAction
        /// and you will get passed the container instance which you can save at that point.
        /// </summary>
        /// <returns></returns>
        public static Container GetContainer()
        {
            return container.Value;
        }
    }
}
