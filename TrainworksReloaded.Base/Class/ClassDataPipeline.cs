using HarmonyLib;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Base.Localization;
using TrainworksReloaded.Core.Enum;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Impl;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Class
{
    public class ClassDataPipeline : IDataPipeline<IRegister<ClassData>, ClassData>
    {
        private readonly PluginAtlas atlas;
        private readonly IModLogger<ClassDataPipeline> logger;
        private readonly IRegister<LocalizationTerm> termRegister;
        private readonly IGuidProvider guidProvider;

        public ClassDataPipeline(
            PluginAtlas atlas,
            IModLogger<ClassDataPipeline> logger,
            IRegister<LocalizationTerm> termRegister,
            IGuidProvider guidProvider
        )
        {
            this.atlas = atlas;
            this.logger = logger;
            this.termRegister = termRegister;
            this.guidProvider = guidProvider;
        }

        public List<IDefinition<ClassData>> Run(IRegister<ClassData> service)
        {
            // We load all cards and then finalize them to avoid dependency loops
            var processList = new List<IDefinition<ClassData>>();
            foreach (var config in atlas.PluginDefinitions)
            {
                processList.AddRange(LoadClasses(service, config.Key, config.Value.Configuration));
            }
            return processList;
        }

        /// <summary>
        /// Loads the Card Definitions in
        /// </summary>
        /// <param name="service"></param>
        /// <param name="key"></param>
        /// <param name="pluginConfig"></param>
        /// <returns></returns>
        private List<ClassDataDefinition> LoadClasses(
            IRegister<ClassData> service,
            string key,
            IConfiguration pluginConfig
        )
        {
            var processList = new List<ClassDataDefinition>();
            foreach (var child in pluginConfig.GetSection("classes").GetChildren())
            {
                var data = LoadClassConfiguration(service, key, child);
                if (data != null)
                {
                    processList.Add(data);
                }
            }
            return processList;
        }

        private ClassDataDefinition? LoadClassConfiguration(
            IRegister<ClassData> service,
            string key,
            IConfiguration configuration
        )
        {
            var id = configuration.GetSection("id").ParseString();
            if (id == null)
            {
                return null;
            }

            var name = key.GetId("Class", id);
            var titleKey = $"ClassData_titleKey-{name}";
            var descriptionKey = $"ClassData_descriptionKey-{name}";
            var subclassDescriptionKey = $"ClassData_subclassDescriptionKey-{name}";
            var overrideMode = configuration.GetSection("override").ParseOverrideMode();

            string guid;
            if (overrideMode.IsOverriding() && service.TryLookupName(id, out ClassData? data, out var _))
            {
                logger.Log(LogLevel.Info, $"Overriding Clan {id}...");
                titleKey = data.GetTitleKey();
                descriptionKey = data.GetDescriptionKey();
                subclassDescriptionKey = data.GetSubclassDescriptionKey();
                guid = data.GetID();
            }
            else
            {
                data = ScriptableObject.CreateInstance<ClassData>();
                data.name = name;
                guid = guidProvider.GetGuidDeterministic(name).ToString();
            }

            //handle id
            AccessTools.Field(typeof(ClassData), "id").SetValue(data, guid);

            //handle titles
            var localizationTitleTerm = configuration.GetSection("titles").ParseLocalizationTerm();
            if (localizationTitleTerm != null)
            {
                AccessTools.Field(typeof(ClassData), "titleLoc").SetValue(data, titleKey);
                localizationTitleTerm.Key = titleKey;
                termRegister.Register(titleKey, localizationTitleTerm);
            }

            //handle desc
            var localizationDescriptionTerm = configuration
                .GetSection("descriptions")
                .ParseLocalizationTerm();
            if (localizationDescriptionTerm != null)
            {
                AccessTools
                    .Field(typeof(ClassData), "descriptionLoc")
                    .SetValue(data, descriptionKey);
                localizationDescriptionTerm.Key = descriptionKey;
                termRegister.Register(descriptionKey, localizationDescriptionTerm);
            }

            //handle desc
            var localizatioSubclassDescriptionTerm = configuration
                .GetSection("subclass_descriptions")
                .ParseLocalizationTerm();
            if (localizatioSubclassDescriptionTerm != null)
            {
                AccessTools
                    .Field(typeof(ClassData), "subclassDescriptionLoc")
                    .SetValue(data, subclassDescriptionKey);
                localizatioSubclassDescriptionTerm.Key = subclassDescriptionKey;
                termRegister.Register(subclassDescriptionKey, localizatioSubclassDescriptionTerm);
            }

            //handel bool
            var corruptionEnabled = data.GetCorruptionEnabled();
            AccessTools
                .Field(typeof(ClassData), "corruptionEnabled")
                .SetValue(
                    data,
                    configuration.GetSection("corruption_enabled").ParseBool() ?? corruptionEnabled
                );

            var isCrew = data.IsCrew();
            AccessTools
                .Field(typeof(ClassData), "isCrew")
                .SetValue(data, configuration.GetSection("is_crew").ParseBool() ?? isCrew);

            var isLaunch = data.IsLaunchClan();
            AccessTools
                .Field(typeof(ClassData), "isLaunchClan")
                .SetValue(data, configuration.GetSection("is_launch_clan").ParseBool() ?? isLaunch);

            var isWurmkin = false;
            AccessTools
                .Field(typeof(ClassData), "isWurmkin")
                .SetValue(data, configuration.GetSection("is_wurmkin").ParseBool() ?? isWurmkin);

            //handle color
            var uiColor = overrideMode.IsNewContent() ? Color.white : data.GetUIColor();
            AccessTools
                .Field(typeof(ClassData), "uiColor")
                .SetValue(data, configuration.GetSection("ui_color").ParseColor() ?? uiColor);

            //handle color
            var uiColorDark = overrideMode.IsNewContent() ? Color.black : data.GetUIColorDark();
            AccessTools
                .Field(typeof(ClassData), "uiColorDark")
                .SetValue(
                    data,
                    configuration.GetSection("ui_color_dark").ParseColor() ?? uiColorDark
                );

            //parse gradient
            var gradient = overrideMode.IsNewContent() ? new Gradient() :
                (Gradient)AccessTools.Field(typeof(ClassData), "uiColorGradient").GetValue(data);
            var gradientKeysConfig = configuration.GetSection("ui_gradient");
            List<GradientColorKey> gradientColorKeys = [];
            foreach (var gradientKeyConfig in gradientKeysConfig.GetChildren())
            {
                var time = gradientKeyConfig.GetSection("time").ParseFloat();
                var color = gradientKeyConfig.GetSection("color").ParseColor() ?? Color.white;
                if (time == null)
                {
                    continue;
                }
                gradientColorKeys.Add(new GradientColorKey(color, (float)time));
            }
            gradient.SetKeys(gradientColorKeys.ToArray(), []);
            AccessTools.Field(typeof(ClassData), "uiColorGradient").SetValue(data, gradient);

            //List<String>
            var classUnlockPreviewTexts =
                (List<string>)
                    AccessTools.Field(typeof(ClassData), "classUnlockPreviewTexts").GetValue(data);
            var previews = configuration.GetDeprecatedSection("class_preview_texts", "class_unlock_preview_texts");
            if (overrideMode == OverrideMode.Replace && previews.Exists())
            {
                classUnlockPreviewTexts.Clear();
            }
            int i = 0;
            foreach (var preview in previews.GetChildren())
            {
                var val = preview.ParseLocalizationTerm();
                if (val == null)
                {
                    continue;
                }
                val.Key = $"ClassData_previewKey-{name}-{i}";
                i++;
                termRegister.Register(val.Key, val);
                classUnlockPreviewTexts.Add(val.Key);
            }

            //register before filling in data using
            var modded = overrideMode.IsNewContent();
            if (modded)
                service.Register(name, data);

            return new ClassDataDefinition(key, data, configuration, !modded);
        }
    }
}
