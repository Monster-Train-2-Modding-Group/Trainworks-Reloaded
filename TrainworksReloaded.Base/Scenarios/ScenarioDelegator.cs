using System.Collections.Generic;

namespace TrainworksReloaded.Base.Scenarios
{
    public class ScenarioDelegator
    {
        public struct ScenarioEntry
        {
            public int Distance { get; set; }
            public string RunType { get; set; }
            public ScenarioData Scenario { get; set; }
        }
        public List<ScenarioEntry> Scenarios = [];

        public void Add(ScenarioData data, int distance, string run_type)
        {
            Scenarios.Add(new ScenarioEntry { Scenario = data, Distance = distance, RunType = run_type });
        }
    }
}
