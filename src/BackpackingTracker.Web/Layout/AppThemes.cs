using BackpackingTracker.Core.Preferences;
using MudBlazor;

namespace BackpackingTracker.Web.Layout;

/// <summary>
/// The app's colour palettes (tickets 30 and 35), each with a light and a dark version, chosen in Settings.
/// </summary>
public static class AppThemes
{
    /// <summary>A palette with its display name and its MudBlazor theme (light and dark).</summary>
    public sealed record Option(ColourPalette Palette, string Name, MudTheme Theme);

    public static readonly IReadOnlyList<Option> All =
    [
        new(ColourPalette.Camping, "Camping", Theme(
            new PaletteLight
            {
                Primary = "#2E7D32", PrimaryDarken = "#1B5E20", PrimaryLighten = "#4CAF50",
                Secondary = "#E65100", SecondaryDarken = "#BF360C", SecondaryLighten = "#F57C00",
                Background = "#FAF3E0", Surface = "#FFFFFF",
                AppbarBackground = "#1B5E20", AppbarText = "#FFFFFF",
                DrawerBackground = "#F1E8D0", DrawerText = "#33691E", DrawerIcon = "#33691E",
                TextPrimary = "#263238", TextSecondary = "#546E7A", ActionDefault = "#2E7D32", Divider = "#D7CCC8",
            },
            new PaletteDark
            {
                Primary = "#4CAF50", PrimaryDarken = "#2E7D32", PrimaryLighten = "#81C784",
                Secondary = "#FF8A65", SecondaryDarken = "#E65100", SecondaryLighten = "#FFAB91", SecondaryContrastText = "#2E1205",
                Background = "#1A2520", Surface = "#263833",
                AppbarBackground = "#1A2520", AppbarText = "#E8F5E9",
                DrawerBackground = "#1E2D27", DrawerText = "#A5D6A7", DrawerIcon = "#A5D6A7",
                TextPrimary = "#E0E0E0", TextSecondary = "#9E9E9E", ActionDefault = "#4CAF50", Divider = "#37474F",
            })),

        new(ColourPalette.AlpineLake, "Alpine Lake", Theme(
            new PaletteLight
            {
                Primary = "#1F6F8B", PrimaryDarken = "#155269", PrimaryLighten = "#4D97B0",
                Secondary = "#D9902F", SecondaryDarken = "#B57421", SecondaryLighten = "#E8AE5E", SecondaryContrastText = "#2B1C05",
                Background = "#F2F5F4", Surface = "#FFFFFF",
                AppbarBackground = "#1B4F63", AppbarText = "#FFFFFF",
                DrawerBackground = "#E6EEEE", DrawerText = "#1B4F63", DrawerIcon = "#1B4F63",
                TextPrimary = "#1E2A30", TextSecondary = "#5A6B73", ActionDefault = "#4A5E66", Divider = "#D3DDDF",
            },
            new PaletteDark
            {
                Primary = "#6CC3D9", PrimaryDarken = "#3E9DB5", PrimaryLighten = "#9ADAE8", PrimaryContrastText = "#08222B",
                Secondary = "#F0B45A", SecondaryContrastText = "#2B1C05",
                Background = "#111B20", Surface = "#1A2A31",
                AppbarBackground = "#142228", AppbarText = "#DDEFF3",
                DrawerBackground = "#15232A", DrawerText = "#A9D6E2", DrawerIcon = "#A9D6E2",
                TextPrimary = "#E3ECEE", TextSecondary = "#9AAEB4", ActionDefault = "#9AAEB4", Divider = "#2C3D44",
            })),

        new(ColourPalette.CanyonSandstone, "Canyon Sandstone", Theme(
            new PaletteLight
            {
                Primary = "#B5532E", PrimaryDarken = "#8E3F21", PrimaryLighten = "#D27650",
                Secondary = "#5E7D5A", SecondaryDarken = "#475F44", SecondaryLighten = "#7E9C7A",
                Background = "#F7F0E6", Surface = "#FFFDF9",
                AppbarBackground = "#7A3A22", AppbarText = "#FFF6EE",
                DrawerBackground = "#EFE3D3", DrawerText = "#6B3420", DrawerIcon = "#6B3420",
                TextPrimary = "#2E211B", TextSecondary = "#74635A", ActionDefault = "#74635A", Divider = "#E2D4C3",
            },
            new PaletteDark
            {
                Primary = "#E58B63", PrimaryDarken = "#C76B44", PrimaryLighten = "#F2AA8A", PrimaryContrastText = "#2A120A",
                Secondary = "#9DBB97", SecondaryContrastText = "#13200F",
                Background = "#1C1512", Surface = "#2A201B",
                AppbarBackground = "#231A16", AppbarText = "#F5E3D6",
                DrawerBackground = "#241B17", DrawerText = "#E9B79E", DrawerIcon = "#E9B79E",
                TextPrimary = "#EEE3DA", TextSecondary = "#B09F94", ActionDefault = "#B09F94", Divider = "#3E322B",
            })),

        new(ColourPalette.MossAndLichen, "Moss & Lichen", Theme(
            new PaletteLight
            {
                Primary = "#4F6B3A", PrimaryDarken = "#3A5129", PrimaryLighten = "#6F8C57",
                Secondary = "#B08D2A", SecondaryDarken = "#8C6F1E", SecondaryLighten = "#C9A94D",
                Background = "#F1F2EA", Surface = "#FFFFFF",
                AppbarBackground = "#33452A", AppbarText = "#F2F5E8",
                DrawerBackground = "#E6E8DC", DrawerText = "#33452A", DrawerIcon = "#33452A",
                TextPrimary = "#23281F", TextSecondary = "#636B5A", ActionDefault = "#636B5A", Divider = "#D8DBCD",
            },
            new PaletteDark
            {
                Primary = "#A3C083", PrimaryDarken = "#7E9C60", PrimaryLighten = "#C2D8A8", PrimaryContrastText = "#17220F",
                Secondary = "#D9B852", SecondaryContrastText = "#2A2005",
                Background = "#151913", Surface = "#20261C",
                AppbarBackground = "#1A1F17", AppbarText = "#E5EBDB",
                DrawerBackground = "#1B2018", DrawerText = "#C2D8A8", DrawerIcon = "#C2D8A8",
                TextPrimary = "#E4E8DD", TextSecondary = "#A2AA97", ActionDefault = "#A2AA97", Divider = "#343C2E",
            })),

        new(ColourPalette.TwilightCampfire, "Twilight Campfire", Theme(
            new PaletteLight
            {
                Primary = "#3D4A7A", PrimaryDarken = "#2C365C", PrimaryLighten = "#5E6C9E",
                Secondary = "#E07A2E", SecondaryDarken = "#B85F1C", SecondaryLighten = "#EC9A5C",
                Background = "#F3F2F7", Surface = "#FFFFFF",
                AppbarBackground = "#2C365C", AppbarText = "#F1F0FA",
                DrawerBackground = "#E8E7F0", DrawerText = "#2C365C", DrawerIcon = "#2C365C",
                TextPrimary = "#222433", TextSecondary = "#62657A", ActionDefault = "#62657A", Divider = "#DCDCE6",
            },
            new PaletteDark
            {
                Primary = "#A5B2E8", PrimaryDarken = "#7F8DCC", PrimaryLighten = "#C5CEF3", PrimaryContrastText = "#161B33",
                Secondary = "#F5A05C", SecondaryContrastText = "#2E1605",
                Background = "#12131C", Surface = "#1C1E2B",
                AppbarBackground = "#161826", AppbarText = "#E3E5F5",
                DrawerBackground = "#171926", DrawerText = "#C5CEF3", DrawerIcon = "#C5CEF3",
                TextPrimary = "#E6E7F0", TextSecondary = "#A0A3B8", ActionDefault = "#A0A3B8", Divider = "#2E3142",
            })),

        new(ColourPalette.Glacier, "Glacier", Theme(
            new PaletteLight
            {
                Primary = "#3B5B7A", PrimaryDarken = "#2A4560", PrimaryLighten = "#5E7F9E",
                Secondary = "#C9553B", SecondaryDarken = "#A5412B", SecondaryLighten = "#DD7B64",
                Background = "#EEF3F6", Surface = "#FFFFFF",
                AppbarBackground = "#2A4560", AppbarText = "#F2F7FB",
                DrawerBackground = "#E2EAF0", DrawerText = "#2A4560", DrawerIcon = "#2A4560",
                TextPrimary = "#1C2A36", TextSecondary = "#5B6E7D", ActionDefault = "#5B6E7D", Divider = "#D2DCE3",
            },
            new PaletteDark
            {
                Primary = "#8EB8DC", PrimaryDarken = "#6A98C0", PrimaryLighten = "#B4D2EC", PrimaryContrastText = "#0C1C2A",
                Secondary = "#F28C74", SecondaryContrastText = "#2E0F07",
                Background = "#0F161C", Surface = "#18222B",
                AppbarBackground = "#121B23", AppbarText = "#DCE8F2",
                DrawerBackground = "#131D25", DrawerText = "#B4D2EC", DrawerIcon = "#B4D2EC",
                TextPrimary = "#E2EAF0", TextSecondary = "#9AABB8", ActionDefault = "#9AABB8", Divider = "#2A3843",
            })),

        new(ColourPalette.TopoMap, "Topo Map", Theme(
            new PaletteLight
            {
                Primary = "#8A5A2B", PrimaryDarken = "#6B4520", PrimaryLighten = "#A9784A",
                Secondary = "#2F6E9E", SecondaryDarken = "#235679", SecondaryLighten = "#5591BE",
                Background = "#F4EEDC", Surface = "#FFFCF3",
                AppbarBackground = "#5C3D1E", AppbarText = "#FBF3E2",
                DrawerBackground = "#EBE2CA", DrawerText = "#5C3D1E", DrawerIcon = "#5C3D1E",
                TextPrimary = "#2D2418", TextSecondary = "#6E604C", ActionDefault = "#6E604C", Divider = "#DDD2B8",
            },
            new PaletteDark
            {
                Primary = "#D6A06A", PrimaryDarken = "#B98148", PrimaryLighten = "#E8C095", PrimaryContrastText = "#2A1906",
                Secondary = "#7FB3DB", SecondaryContrastText = "#0A1D2C",
                Background = "#1A1712", Surface = "#26211A",
                AppbarBackground = "#1F1B15", AppbarText = "#F0E4CF",
                DrawerBackground = "#201C16", DrawerText = "#E8C095", DrawerIcon = "#E8C095",
                TextPrimary = "#ECE3D2", TextSecondary = "#AE9F88", ActionDefault = "#AE9F88", Divider = "#3A3227",
            })),

        new(ColourPalette.AutumnLarch, "Autumn Larch", Theme(
            new PaletteLight
            {
                Primary = "#7A2E3B", PrimaryDarken = "#5A1F2A", PrimaryLighten = "#9C4A58",
                Secondary = "#B8860B", SecondaryDarken = "#8F6808", SecondaryLighten = "#D4A72E",
                Background = "#F7F1EC", Surface = "#FFFFFF",
                AppbarBackground = "#5A1F2A", AppbarText = "#FBEFF1",
                DrawerBackground = "#EFE4DE", DrawerText = "#5A1F2A", DrawerIcon = "#5A1F2A",
                TextPrimary = "#2B1D20", TextSecondary = "#6E5A5E", ActionDefault = "#6E5A5E", Divider = "#E3D6D0",
            },
            new PaletteDark
            {
                Primary = "#E2909C", PrimaryDarken = "#C86F7D", PrimaryLighten = "#F0B5BE", PrimaryContrastText = "#2E0C12",
                Secondary = "#E8C150", SecondaryContrastText = "#2A1F02",
                Background = "#1A1214", Surface = "#261B1E",
                AppbarBackground = "#1F1517", AppbarText = "#F3E2E5",
                DrawerBackground = "#201618", DrawerText = "#F0B5BE", DrawerIcon = "#F0B5BE",
                TextPrimary = "#EDE1E3", TextSecondary = "#B39FA3", ActionDefault = "#B39FA3", Divider = "#3D2D31",
            })),

        new(ColourPalette.TrailBlaze, "Trail Blaze", Theme(
            new PaletteLight
            {
                Primary = "#B84A00", PrimaryDarken = "#923B00", PrimaryLighten = "#E0661A",
                Secondary = "#2F5F5A", SecondaryDarken = "#224844", SecondaryLighten = "#4A807A",
                Background = "#F4F4F2", Surface = "#FFFFFF",
                AppbarBackground = "#222628", AppbarText = "#FFFFFF",
                DrawerBackground = "#E8E9E7", DrawerText = "#222628", DrawerIcon = "#222628",
                TextPrimary = "#1F2326", TextSecondary = "#5C6368", ActionDefault = "#5C6368", Divider = "#DADCDD",
            },
            new PaletteDark
            {
                Primary = "#FF8A3D", PrimaryDarken = "#E86F1E", PrimaryLighten = "#FFAD75", PrimaryContrastText = "#2B1100",
                Secondary = "#7FB8B0", SecondaryContrastText = "#0B201D",
                Background = "#121416", Surface = "#1C1F22",
                AppbarBackground = "#0E1012", AppbarText = "#F2F2F2",
                DrawerBackground = "#15181A", DrawerText = "#FFAD75", DrawerIcon = "#FFAD75",
                TextPrimary = "#ECECEC", TextSecondary = "#A3A8AC", ActionDefault = "#A3A8AC", Divider = "#2E3236",
            })),

        new(ColourPalette.NorthernLights, "Northern Lights", Theme(
            new PaletteLight
            {
                Primary = "#16806F", PrimaryDarken = "#0F5F53", PrimaryLighten = "#3AA08E",
                Secondary = "#A8387A", SecondaryDarken = "#842B5F", SecondaryLighten = "#C45F9A",
                Background = "#EFF5F4", Surface = "#FFFFFF",
                AppbarBackground = "#0F3B40", AppbarText = "#E8FBF7",
                DrawerBackground = "#E2EDEB", DrawerText = "#0F3B40", DrawerIcon = "#0F3B40",
                TextPrimary = "#17282A", TextSecondary = "#56696B", ActionDefault = "#56696B", Divider = "#D0DFDD",
            },
            new PaletteDark
            {
                Primary = "#5FE0C2", PrimaryDarken = "#36C2A3", PrimaryLighten = "#93ECD7", PrimaryContrastText = "#04231C",
                Secondary = "#E58CC0", SecondaryContrastText = "#2E0A1E",
                Background = "#0B1216", Surface = "#131D22",
                AppbarBackground = "#0A1014", AppbarText = "#D9F7EF",
                DrawerBackground = "#0D161A", DrawerText = "#93ECD7", DrawerIcon = "#93ECD7",
                TextPrimary = "#E1ECEC", TextSecondary = "#94A9AB", ActionDefault = "#94A9AB", Divider = "#24343A",
            })),
    ];

    /// <summary>The chosen palette; Camping if the saved one is unknown (such as one from a newer version).</summary>
    public static Option For(ColourPalette palette) => All.FirstOrDefault(option => option.Palette == palette) ?? All[0];

    private static MudTheme Theme(PaletteLight light, PaletteDark dark) => new() { PaletteLight = light, PaletteDark = dark };
}
