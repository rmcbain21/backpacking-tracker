using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Preferences;

/// <summary>Whether the palette's light or dark version is shown.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Appearance>))]
public enum Appearance
{
    /// <summary>Follows the device's dark mode setting.</summary>
    MatchDevice,

    Light,

    Dark,
}
