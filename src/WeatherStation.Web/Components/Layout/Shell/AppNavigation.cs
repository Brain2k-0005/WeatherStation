using Lumeo;
using Lumeo.Icons;
using WeatherStation.Web.Services;

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
    // Unterseiten (z. B. /muster/observer) zählen zum Eintrag ihrer Hauptseite (/muster),
    // damit der Sidebar-Eintrag dort aktiv bleibt.
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

        foreach (NavItem item in Items)
        {
            // "/" ausnehmen, sonst wäre jede Adresse eine Unterseite der Startseite.
            if (item.Href != "/" && path.StartsWith(item.Href + "/", StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }
        return null;
    }

    // Titel für die Kopfleiste. Lernseiten zeigen "Design Patterns / Observer",
    // alle anderen Seiten den Namen ihres Sidebar-Eintrags.
    public static string TitleFor(string baseRelativePath)
    {
        NavItem? item = Match(baseRelativePath);
        if (item is null)
        {
            return "Wetterstation";
        }

        string path = "/" + baseRelativePath.Split('?', '#')[0].Trim('/');
        foreach (PatternInfo pattern in PatternCatalog.All)
        {
            if (string.Equals(pattern.Href, path, StringComparison.OrdinalIgnoreCase))
            {
                return item.Label + " / " + pattern.Title;
            }
        }
        return item.Label;
    }
}
