using WeatherStation.Core.Models;

namespace WeatherStation.ConsoleApp.Observers;

// ============================================================
// PATTERN: Observer – Rolle: konkreter Observer (Warnungen)
// Hört auf den WarningService (dieser ist selbst ein Subject).
// Die Farbe hängt von der Warnstufe ab.
// ============================================================
public sealed class WarningPrinter : IObserver<WeatherWarning>
{
    public void OnNext(WeatherWarning warning)
    {
        Console.ForegroundColor = ColorFor(warning.Level);
        Console.WriteLine($"  >> {warning.Level.ToGerman().ToUpperInvariant()}: {warning.Title} – {warning.Message}");
        Console.ResetColor();

        if (warning.Level == WarningLevel.Danger)
        {
            TryBeep();
        }
    }

    public void OnError(Exception error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [Warn-Anzeige] Fehler: {error.Message}");
        Console.ResetColor();
    }

    public void OnCompleted()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  [Warn-Anzeige] Warn-Dienst beendet (OnCompleted).");
        Console.ResetColor();
    }

    private static ConsoleColor ColorFor(WarningLevel level)
    {
        switch (level)
        {
            case WarningLevel.Danger:
                return ConsoleColor.Red;
            case WarningLevel.Warning:
                return ConsoleColor.Yellow;
            default:
                return ConsoleColor.Cyan;
        }
    }

    // Console.Beep gibt es nur unter Windows; ein Fehler darf die Anzeige nie stoppen.
    private static void TryBeep()
    {
        if (!OperatingSystem.IsWindows() || Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            Console.Beep();
        }
        catch (Exception)
        {
            // Kein Lautsprecher vorhanden – der Ton ist nur ein Extra, daher bewusst ignoriert.
        }
    }
}
