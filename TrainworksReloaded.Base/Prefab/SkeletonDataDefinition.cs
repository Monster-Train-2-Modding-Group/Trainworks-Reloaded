using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Spine;
using Spine.Unity;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class SkeletonDataDefinition(string key, SkeletonDataAsset data, IConfiguration configuration) : IDefinition<SkeletonDataAsset>
    {
        public string Key { get; set; } = key;
        public SkeletonDataAsset Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
