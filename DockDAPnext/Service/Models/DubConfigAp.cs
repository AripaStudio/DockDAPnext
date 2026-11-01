using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DockDAPnext.Service
{
    // In-memory shape of dub.json. Keys that are not modeled here survive untouched
    // through OtherData (JsonExtensionData), so loading and saving never drops data.
    public class DubConfigAp
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("authors")] public List<string> Authors { get; set; } = new List<string>();

        [JsonProperty("license")] public string License { get; set; }

        [JsonProperty("targetName")] public string TargetName { get; set; }

        [JsonProperty("targetType")] public string TargetType { get; set; }

        [JsonProperty("sourceFiles")] public List<string> SourceFiles { get; set; } = new List<string>();

        [JsonProperty("dependencies")]
        public Dictionary<string, JToken> Dependencies { get; set; } = new Dictionary<string, JToken>();

        // dub's real recipe field is 'configurations' (chosen with --config=...). Older
        // versions of this extension wrote 'buildConfigurations', a key dub ignores;
        // DubManagerAp.ReadDubFileAp migrates such files so the UI drives real dub builds.
        [JsonProperty("configurations")]
        public List<BuildConfiguration> BuildConfigurations { get; set; } = new List<BuildConfiguration>();

        // dub's package-level targetPath: the fallback output folder (Output) that a
        // configuration can override (debug -> Output/debug, release -> Output/release).
        [JsonProperty("targetPath")]
        public string TargetPath { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> OtherData { get; set; } = new Dictionary<string, object>();
    }
}
