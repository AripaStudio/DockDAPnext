using Newtonsoft.Json;
using System.Collections.Generic;

namespace DockDAPnext.Service
{
    // One entry of dub.json's 'configurations' list: name plus the build settings
    // that configuration adds on top of the package-level settings.
    public class BuildConfiguration
    {
        [JsonProperty("name")] public string Name { get; set; }

        // Output folder of this configuration, relative to the dub.json folder. dub only
        // honors it when the list lives under the real 'configurations' key.
        [JsonProperty("targetPath", NullValueHandling = NullValueHandling.Ignore)] public string TargetPath { get; set; }

        [JsonProperty("dflags")] public List<string> DFlags { get; set; }

        [JsonProperty("lflags")] public List<string> LFlags { get; set; }

        [JsonProperty("versions")] public List<string> Versions { get; set; }

        [JsonProperty("buildOptions")] public List<BuildOptionTypes> BuildOptions { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> OtherDataBuildConfiguration { get; set; } = new Dictionary<string, object>();
    }
}
