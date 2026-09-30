using Lumeo;
using Lumeo.Icons;

namespace WeatherStation.Web.Components.Layout.Shell;

// Eine einzige Liste für Sidebar UND Seitentitel in der Kopfleiste,
// damit beide immer denselben Namen zeigen.
public static class AppNavigation
{
    public sealed record NavItem(string Href, string Label, IconSource Icon);

    public static readonly IReadOnlyList<NavItem> Items =
    [
        new("/", "Übersicht", Lucide.LayoutDashboard),
        new("/warnungen", "Warnungen", Lucide.TriangleAlert),
        new("/observer-labor", "Observer-Labor", Lucide.FlaskConical),
        new("/muster", "Design Patterns", Lucide.Blocks),
    ];

    // Findet den Eintrag zur aktuellen Adresse ("" = Startseite).
    public static NavItem? Match(string baseRelativePath)
    {
        string path = "/" + baseRelativePath.Split('?', '#')[0].Trim('/');

        foreach (NavItem item in Items)
        {
            if (string.Equals(item.Href, path, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }
        return null;
    }
}
