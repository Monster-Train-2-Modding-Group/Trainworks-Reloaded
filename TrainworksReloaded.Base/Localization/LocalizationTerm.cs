using System.Collections.Generic;

namespace TrainworksReloaded.Base.Localization
{
    public class LocalizationTerm
    {
        public string Key { get; set; } = "";
        public string Type { get; set; } = "";
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

        public bool HasTranslation()
        {
            return !(English == "" && French == "" && German == "" && Russian == "" && Portuguese == "" && Chinese == "" && Spanish == "" && ChineseTraditional == "" && Korean == "" && Japanese == "" && OtherLanguages.Count == 0);
        }
    }
}
