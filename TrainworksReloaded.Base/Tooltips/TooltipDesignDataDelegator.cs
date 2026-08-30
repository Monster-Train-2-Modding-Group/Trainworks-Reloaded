using System;
using System.Collections.Generic;
using System.Text;

namespace TrainworksReloaded.Base.Tooltips
{
    public class TooltipDesignDataDelegator
    {
        internal Dictionary<TooltipDesigner.TooltipDesignType, TooltipDesigner.TooltipDesignData> tooltipDesigns = [];

        public void Add(TooltipDesigner.TooltipDesignData data)
        {
            tooltipDesigns.Add(data.tooltipDesignType, data);
        }

        public TooltipDesigner.TooltipDesignData? Get(TooltipDesigner.TooltipDesignType type)
        {
            return tooltipDesigns.GetValueOrDefault(type);
        }
    }
}
