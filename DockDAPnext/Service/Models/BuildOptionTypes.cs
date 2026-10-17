using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DockDAPnext.Service
{
    // dub's buildOptions vocabulary; serialized as the plain names dub expects.
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BuildOptionTypes
    {
        debugMode,
        releaseMode,
        coverage,
        unoptimized,
        inline,
        noBoundsCheck,
        debugInfo,
        optimize,
        warnings,
        warningsAsErrors
    }
}
