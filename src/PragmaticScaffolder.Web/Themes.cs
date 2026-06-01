using MudBlazor;

namespace PragmaticScaffolder.Web;

internal static class Themes
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#594AE2",
            Secondary = "#FF4081",
            AppbarBackground = "#594AE2"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7B6FF0",
            Secondary = "#FF4081",
            AppbarBackground = "#27212E",
            Surface = "#1E1E2D",
            Background = "#16151E"
        }
    };

    public static MudTheme Blue { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1565C0",
            Secondary = "#0288D1",
            AppbarBackground = "#1565C0"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#42A5F5",
            Secondary = "#0288D1",
            AppbarBackground = "#0D1B2A",
            Surface = "#1A2744",
            Background = "#101828"
        }
    };

    public static MudTheme Green { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2E7D32",
            Secondary = "#F57F17",
            AppbarBackground = "#2E7D32"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#66BB6A",
            Secondary = "#FFD54F",
            AppbarBackground = "#1B2E1B",
            Surface = "#1E2E1E",
            Background = "#141F14"
        }
    };

    public static MudTheme Orange { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#E65100",
            Secondary = "#FDD835",
            AppbarBackground = "#E65100"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#FFA726",
            Secondary = "#FDD835",
            AppbarBackground = "#2D1B00",
            Surface = "#2E1E00",
            Background = "#1E1500"
        }
    };

    public static MudTheme Purple { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#6A1B9A",
            Secondary = "#00BCD4",
            AppbarBackground = "#6A1B9A"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#CE93D8",
            Secondary = "#00BCD4",
            AppbarBackground = "#1A0A2E",
            Surface = "#1E1030",
            Background = "#130A1E"
        }
    };
}
