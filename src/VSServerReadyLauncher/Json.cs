using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace VSServerReadyLauncher.Json
{
    public class ServerReadyAction
    {
        [JsonProperty("outputPattern", Required = Required.Always)]
        public string OutputPattern { get; set; }

        [JsonProperty("projectToLaunch", Required = Required.Always)]
        public string ProjectToLaunch;
    }

    class SettingsFile
    {
        [JsonProperty("serverReadyActions", Required = Required.Always)]
        public IReadOnlyCollection<ServerReadyAction> ServerReadyActions { get; set; }
    }
}
