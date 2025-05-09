using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Prefab
{
    public class AssetBundleDefinition(string key, AssetBundle data, IConfiguration configuration) : IDefinition<AssetBundle>
    {
        public string Key { get; set; } = key;
        public AssetBundle Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
