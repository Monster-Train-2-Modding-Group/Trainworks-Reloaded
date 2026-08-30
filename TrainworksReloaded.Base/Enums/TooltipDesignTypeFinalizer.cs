using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Prefab;
using TrainworksReloaded.Base.Tooltips;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Enums
{
    public class TooltipDesignTypeFinalizer : IDataFinalizer
    {
        private readonly IModLogger<TooltipDesignTypeFinalizer> logger;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly TooltipDesignDataDelegator delegator;
        private readonly ICache<IDefinition<TooltipDesigner.TooltipDesignType>> cache;

        public TooltipDesignTypeFinalizer(
            IModLogger<TooltipDesignTypeFinalizer> logger,
            IRegister<Sprite> spriteRegister,
            TooltipDesignDataDelegator delegator,
            ICache<IDefinition<TooltipDesigner.TooltipDesignType>> cache
        )
        {
            this.logger = logger;
            this.spriteRegister = spriteRegister;
            this.delegator = delegator;
            this.cache = cache;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeTrigger(definition);
            }
            cache.Clear();
        }

        private void FinalizeTrigger(IDefinition<TooltipDesigner.TooltipDesignType> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var style = definition.Data;
            var id = definition.Id;

            logger.Log(LogLevel.Info, $"Finalizing TooltipDesignType {key} {id} path: {configuration.GetPath()}...");

            TooltipDesigner.TooltipDesignData data = new();

            AccessTools.Field(typeof(TooltipDesigner.TooltipDesignData), "_tooltipDesignType").SetValue(data, style);

            var spriteReference = configuration.GetSection("background").ParseReference();
            if (
                spriteReference != null
                && spriteRegister.TryLookupId(
                    spriteReference.ToId(key, TemplateConstants.Sprite),
                    out var lookup,
                    out var _,
                    spriteReference.context
                )
            )
            {
                AccessTools.Field(typeof(TooltipDesigner.TooltipDesignData), "_backgroundSprite").SetValue(data, lookup);
            }

            FontStyles styles = FontStyles.Normal;
            foreach (var font_style in configuration.GetSection("font_styles").GetChildren().Select(x => x.ParseString()))
            {
                if (font_style == null)
                    continue;
                styles |= font_style.ToLower() switch
                {
                    "bold" => FontStyles.Bold,
                    "italic" => FontStyles.Italic,
                    "underline" => FontStyles.Underline,
                    "lowercase" => FontStyles.LowerCase,
                    "uppercase" => FontStyles.UpperCase,
                    "smallcaps" => FontStyles.SmallCaps,
                    "strikethrough" => FontStyles.Strikethrough,
                    "superscript" => FontStyles.Superscript,
                    "subscript" => FontStyles.Subscript,
                    "highlight" => FontStyles.Highlight,
                    _ => FontStyles.Normal
                };
            }
            AccessTools.Field(typeof(TooltipDesigner.TooltipDesignData), "_fontStyle").SetValue(data, styles);

            uint additionalTextSize = (uint)(configuration.GetSection("additional_text_relative_size").ParseInt() ?? 100);
            if (additionalTextSize > 100)
                additionalTextSize = 100;
            AccessTools.Field(typeof(TooltipDesigner.TooltipDesignData), "_additionalTextRelativeSize").SetValue(data, additionalTextSize);

            Color fontColor = configuration.GetSection("font_color").ParseColor() ?? Color.white;
            AccessTools.Field(typeof(TooltipDesigner.TooltipDesignData), "_fontColor").SetValue(data, fontColor);

            TooltipDesigner.TooltipWidth tooltipWidth = configuration.GetSection("tooltip_width").ParseString()?.ToLower() switch
                {
                    "default" => TooltipDesigner.TooltipWidth.Default,
                    "wide" => TooltipDesigner.TooltipWidth.Wide,
                    "megawide" => TooltipDesigner.TooltipWidth.MegaWide,
                    "card_details" => TooltipDesigner.TooltipWidth.CardDetails,
                    _ => TooltipDesigner.TooltipWidth.Default,
                };
            AccessTools.Field(typeof(TooltipDesigner.TooltipDesignData), "_width").SetValue(data, tooltipWidth);

            Vector2Int padding = configuration.GetSection("padding").ParseVec2Int();
            AccessTools.Field(typeof(TooltipDesigner.TooltipDesignData), "_contentPadding").SetValue(data, padding);

            delegator.Add(data);
        }
    }
}
