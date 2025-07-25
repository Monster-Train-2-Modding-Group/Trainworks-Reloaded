using System.Collections.Generic;

namespace TrainworksReloaded.Base.Map
{
    public record MapNodeKey
    {
        public MapNodeKey() { }

        public MapNodeKey(string runKey, string bucketKey)
        {
            RunKey = runKey;
            BucketKey = bucketKey;
        }

        public string RunKey { get; set; } = "";
        public string BucketKey { get; set; } = "";
    }

    public class MapNodeDelegator
    {
        public Dictionary<MapNodeKey, List<MapNodeData>> MapBucketToData = [];
    }
}
