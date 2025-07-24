using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Base.Tooltips
{
    public class AdditionalTooltipPipeline : IDataPipeline<IRegister<AdditionalTooltipData>, AdditionalTooltipData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<AdditionalTooltipPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;

        public AdditionalTooltipPipeline(PluginAtlas atlas, IModLogger<AdditionalTooltipPipeline> logger, IRegister<LocalizationTerm> termRegister)
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
        }

        public List<IDefinition<AdditionalTooltipData>> Run(IRegister<AdditionalTooltipData> service)
        {
            var definitions = new List<IDefinition<AdditionalTooltipData>>();
            foreach (var pluginDefinition in atlas.PluginDefinitions)
            {
                var key = pluginDefinition.Key;
                foreach (var config in pluginDefinition.Value.Configuration.GetSection("additional_tooltips").GetChildren())
                {
                    var item = ProcessItem(service, key, config);
                    if (item != null)
                        definitions.Add(item);
                }
            }
            return definitions;
        }

        public IDefinition<AdditionalTooltipData>? ProcessItem(IRegister<AdditionalTooltipData> service, string key, IConfiguration configuration)
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId(TemplateConstants.AdditionalTooltip, id);
            var data = new AdditionalTooltipData();

            var titleKeyTerm = configuration.GetSection("titles").ParseLocalizationTerm();
            if (titleKeyTerm != null)
            {
                string tooltipKey = $"AdditionalTooltipData_tooltipTitleKey-{name}";
                if (titleKeyTerm.Key.IsNullOrEmpty())
                    titleKeyTerm.Key = tooltipKey;
                data.titleKey = titleKeyTerm.Key;
                if (titleKeyTerm.HasTranslation())
                    termRegister.Register(titleKeyTerm.Key, titleKeyTerm);
            }
            var descriptionTKeyTerm = configuration.GetSection("descriptions").ParseLocalizationTerm();
            if (descriptionTKeyTerm != null)
            {
                string tooltipKey = $"AdditionalTooltipData_tooltipDescriptionKey-{name}";
                if (descriptionTKeyTerm.Key.IsNullOrEmpty())
                    descriptionTKeyTerm.Key = tooltipKey;
                data.descriptionKey = descriptionTKeyTerm.Key;
                if (descriptionTKeyTerm.HasTranslation())
                    termRegister.Register(descriptionTKeyTerm.Key, descriptionTKeyTerm);
            }

            // data.trigger is processed in the finalizer
            // data.status_id is processed in the finalizer
            data.style = configuration.GetSection("style").ParseTooltipDesignType() ?? TooltipDesigner.TooltipDesignType.Keyword;
            data.allowSecondaryPlacement = configuration.GetSection("allow_secondary_placement").ParseBool() ?? false;
            data.hideInTrainRoomUI = configuration.GetSection("hide_in_train_room_ui").ParseBool() ?? false;

            service.Register(name, data);
            return new AdditionalTooltipDefinition(key, data, configuration)
            {
                Id = id,
            };
        }
    }
}
