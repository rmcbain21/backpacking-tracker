using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Preferences;

/// <summary>The app's colours, each with a light and a dark version. Stored by name, so members can be reordered.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ColourPalette>))]
public enum ColourPalette
{
    /// <summary>Forest green and burnt orange on cream, from the earlier PackUp app.</summary>
    Camping,

    /// <summary>Lake teal-blue and larch gold.</summary>
    AlpineLake,

    /// <summary>Terracotta and sage on sand.</summary>
    CanyonSandstone,

    /// <summary>Moss green and lichen ochre on stone.</summary>
    MossAndLichen,

    /// <summary>Dusk indigo and campfire orange.</summary>
    TwilightCampfire,

    /// <summary>Slate blue and coral on icy grey.</summary>
    Glacier,

    /// <summary>Contour brown and water blue on map paper.</summary>
    TopoMap,

    /// <summary>Burgundy and larch gold.</summary>
    AutumnLarch,

    /// <summary>Safety orange and charcoal.</summary>
    TrailBlaze,

    /// <summary>Aurora teal and magenta.</summary>
    NorthernLights,
}
