using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Enums
{
    public class TooltipDesignTypeDefinition(
        string key,
        TooltipDesigner.TooltipDesignType data,
        IConfiguration configuration
    ) : IDefinition<TooltipDesigner.TooltipDesignType>
    {
        public string Key { get; set; } = key;
        public TooltipDesigner.TooltipDesignType Data { get; set; } = data;
        public IConfiguration Configuration { get; set; } = configuration;
        public string Id { get; set; } = "";
        public bool IsModded => true;
    }
}
