using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Preferences;

/// <summary>How large the app's text is, as a share of the browser's normal size. Stored by name, so members can be reordered.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TextSize>))]
public enum TextSize
{
    /// <summary>81.25%: 13px body text.</summary>
    ExtraSmall,

    /// <summary>87.5%: 14px body text.</summary>
    Small,

    /// <summary>100%: 16px body text.</summary>
    Default,

    /// <summary>112.5%: 18px body text.</summary>
    Large,
}
