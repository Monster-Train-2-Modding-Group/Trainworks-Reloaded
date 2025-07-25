using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Map
{
    public class BucketMapNodePipelineDecorator : IDataPipeline<IRegister<MapNodeData>, MapNodeData>
    {
        private readonly IDataPipeline<IRegister<MapNodeData>, MapNodeData> decoratee;
        private readonly MapNodeDelegator delegator;

        public BucketMapNodePipelineDecorator(
            IDataPipeline<IRegister<MapNodeData>, MapNodeData> decoratee,
            MapNodeDelegator delegator
        )
        {
            this.decoratee = decoratee;
            this.delegator = delegator;
        }

        public List<IDefinition<MapNodeData>> Run(IRegister<MapNodeData> service)
        {
            var definitions = decoratee.Run(service);
            foreach (var definition in definitions)
            {
                var data = definition.Data;
                var buckets = definition
                    .Configuration.GetSection("buckets")
                    .GetChildren()
                    .Where(xs => xs.Exists())
                    .Select(xs => new MapNodeKey()
                    {
                        BucketKey = xs.GetSection("bucket").ParseString() ?? "Merchant Ring 1",
                        RunKey = xs.GetSection("run_type").ParseString() ?? "primary",
                    })
                    .ToList()!;
                foreach (var bucket in buckets)
                {
                    if (delegator.MapBucketToData.ContainsKey(bucket))
                    {
                        delegator.MapBucketToData[bucket].Add(data);
                    }
                    else
                    {
                        delegator.MapBucketToData.Add(bucket, [data]);
                    }
                }
            }
            return definitions;
        }
    }
}
