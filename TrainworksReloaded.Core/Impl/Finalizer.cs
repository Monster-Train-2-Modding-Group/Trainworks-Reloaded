using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Core.Impl
{
    public class Finalizer
    {
        private readonly IEnumerable<IDataFinalizer> dataFinalizers;

        public Finalizer(IEnumerable<IDataFinalizer> dataFinalizers)
        {
            this.dataFinalizers = dataFinalizers;
        }

        public void FinalizeData()
        {
            foreach (var final in dataFinalizers)
            {
                final.FinalizeData();
            }
        }
    }
}
