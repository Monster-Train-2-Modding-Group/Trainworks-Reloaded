using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Map
{
    public class RewardNodeDataPipelineDecorator
        : IDataPipeline<IRegister<MapNodeData>, MapNodeData>
    {
        private readonly IDataPipeline<IRegister<MapNodeData>, MapNodeData> decoratee;

        public RewardNodeDataPipelineDecorator(
            IDataPipeline<IRegister<MapNodeData>, MapNodeData> decoratee
        )
        {
            this.decoratee = decoratee;
        }

        public List<IDefinition<MapNodeData>> Run(IRegister<MapNodeData> service)
        {
            var definitions = decoratee.Run(service);
            foreach (var definition in definitions)
            {
                var data1 = definition.Data;
                var configuration1 = definition.Configuration;
                if (data1 is RewardNodeData data)
                {
                    var configuration = configuration1
                        .GetSection("extensions")
                        .GetChildren()
                        .Where(xs => xs.GetSection("reward").Exists())
                        .Select(xs => xs.GetSection("reward"))
                        .First();
                    if (configuration == null)
                        continue;

                    //bool
                    AccessTools
                        .Field(typeof(RewardNodeData), "OverrideTooltipTitleBody")
                        .SetValue(
                            data,
                            configuration.GetDeprecatedSection("override_tooltip_with_reward", "override_tooltip_title_body").ParseBool()
                                ?? false
                        );

                    AccessTools
                        .Field(typeof(RewardNodeData), "UseFormattedOverrideTooltipTitle")
                        .SetValue(
                            data,
                            configuration.GetDeprecatedSection("use_formatted_override_title", "use_formatted_override_tooltip_title").ParseBool()
                                ?? false
                        );

                    AccessTools
                        .Field(typeof(RewardNodeData), "grantImmediately")
                        .SetValue(
                            data,
                            configuration.GetSection("grant_immediately").ParseBool() ?? false
                        );
                }
            }
            return definitions;
        }
    }
}
