using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrainworksReloaded.Base.Localization
{
    public class LocalizationTerm
    {
        public string Key { get; set; } = "";
        public eTermType Type { get; set; } = eTermType.Text;
        public string Desc { get; set; } = "";
        public string Group { get; set; } = "";
        public string Descriptions { get; set; } = "";
        public string English { get; set; } = "";
        public string French { get; set; } = "";
        public string German { get; set; } = "";
        public string Russian { get; set; } = "";
        public string Portuguese { get; set; } = "";
        public string Chinese { get; set; } = "";
        public string Spanish { get; set; } = "";
        public string ChineseTraditional { get; set; } = "";
        public string Korean { get; set; } = "";
        public string Japanese { get; set; } = "";
        public Dictionary<string, string> OtherLanguages { get; set; } = [];
        public int SourceIndex { get; set; } = 0;

        public bool HasTranslation()
        {
            return !(English == "" && French == "" && German == "" && Russian == "" && Portuguese == "" && Chinese == "" && Spanish == "" && ChineseTraditional == "" && Korean == "" && Japanese == "" && OtherLanguages.Count == 0);
        }

        public void Format(IEnumerable<string> objects)
        {
            object[] args = objects?.Cast<object>().ToArray() ?? [];

            English = string.Format(English, args);
            French = string.Format(French, args);
            German = string.Format(German, args);
            Russian = string.Format(Russian, args);
            Portuguese = string.Format(Portuguese, args);
            Chinese = string.Format(Chinese, args);
            Spanish = string.Format(Spanish, args);
            ChineseTraditional = string.Format(ChineseTraditional, args);
            Korean = string.Format(Korean, args);
            Japanese = string.Format(Japanese, args);

            foreach (var key in OtherLanguages.Keys.ToList())
            {
                OtherLanguages[key] = string.Format(OtherLanguages[key], args);
            }
        }
    }
}
