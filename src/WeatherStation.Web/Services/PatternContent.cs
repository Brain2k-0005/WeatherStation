namespace WeatherStation.Web.Services;

// Die Texte der Pattern-Lernseiten (/muster und /muster/...).
// Gleiche Gliederung wie docs/patterns/*.md, nur etwas gekürzt. Die Seiten selbst
// (PatternDetail.razor) enthalten kein Fließtext-Literal, sondern lesen alles von hier.
// In Texten wird `so` als Code und **so** als fett dargestellt (siehe Rich.razor).

public sealed record AnalogyRow(string Everyday, string Project);

public sealed record CodeExample(string Caption, string Code);

public sealed record PatternRole(string Role, string Class, string File);

public sealed record QuizQuestion(string Question, string Answer);

public sealed record PatternInfo
{
    public required string Slug { get; init; }
    public required string Title { get; init; }

    // Übersichtskarte
    public required string OneLiner { get; init; }
    public required string WhenNeeded { get; init; }

    // Detailseite
    public required string DocFile { get; init; }
    public required string Sentence { get; init; }
    public required string AnalogyText { get; init; }
    public required IReadOnlyList<AnalogyRow> Analogy { get; init; }
    public required string DiagramCaption { get; init; }

    public required string WithoutIntro { get; init; }
    public required CodeExample WithoutCode { get; init; }
    public required IReadOnlyList<string> Problems { get; init; }

    public required string WithIntro { get; init; }
    public required IReadOnlyList<CodeExample> WithCode { get; init; }

    public required string StepsIntro { get; init; }
    public required IReadOnlyList<string> Steps { get; init; }

    public required IReadOnlyList<PatternRole> Roles { get; init; }
    public required IReadOnlyList<string> Pros { get; init; }
    public required IReadOnlyList<string> Cons { get; init; }
    public required IReadOnlyList<string> Mistakes { get; init; }
    public required IReadOnlyList<QuizQuestion> Quiz { get; init; }

    public string Href => "/muster/" + Slug;
}

public static class PatternCatalog
{
    // Als Eigenschaft statt Feld: Die Muster unten sind static-Felder, die erst nach dieser Zeile
    // initialisiert werden. Beim Aufruf (nicht beim Klassenstart) sind sie garantiert da.
    public static IReadOnlyList<PatternInfo> All => [Observer, Builder, FactoryMethod, Singleton];

    public static PatternInfo? Find(string slug) =>
        All.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

    // Das Muster, das im Lernpfad auf dieses folgt (null beim letzten).
    public static PatternInfo? Next(PatternInfo current)
    {
        int index = -1;
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Slug == current.Slug)
            {
                index = i;
            }
        }
        return index >= 0 && index + 1 < All.Count ? All[index + 1] : null;
    }

    // ---------------------------------------------------------------
    // Observer
    // ---------------------------------------------------------------

    private static readonly PatternInfo Observer = new()
    {
        Slug = "observer",
        Title = "Observer",
        OneLiner = "Einer meldet, viele hören zu: Das Subject sagt allen angemeldeten Observern Bescheid.",
        WhenNeeded = "Wenn sich bei einem Objekt etwas ändert und mehrere andere darauf reagieren sollen, ohne dass das Objekt sie kennt (Anzeige, Warnung, Statistik, Log).",
        DocFile = "docs/patterns/Observer.md",
        Sentence = "Ein **Subject** (das beobachtete Objekt) führt eine Liste von **Observern** (Zuhörern) und sagt ihnen automatisch Bescheid, sobald es etwas Neues gibt. Es muss die Zuhörer dafür nicht näher kennen.",
        AnalogyText = "Du willst wissen, wann dein Lieblings-YouTuber ein neues Video hochlädt. Du könntest alle fünf Minuten nachschauen. Das nervt. Besser: Du abonnierst den Kanal und bekommst eine Nachricht. Keine Lust mehr? Du kündigst das Abo. Der YouTuber weiß nicht, wer genau zuschaut. Er sagt nur: „Neues Video da!“ Ein zweites Bild ist die Türklingel: Der Knopf muss nicht wissen, wer im Haus wohnt.",
        Analogy =
        [
            new("YouTube-Kanal", "`Station` (meldet neue Messwerte)"),
            new("Abonnent", "Observer, z. B. `WarningHistory`, `WeatherStatistics`, eine Blazor-Seite"),
            new("Abonnieren", "`station.Subscribe(observer)`"),
            new("Kündigen", "`station.Unsubscribe(observer)`"),
            new("„Neues Video!“", "`NotifyObservers(reading)`"),
        ],
        DiagramCaption = "So fließt ein Messwert durch das System. Jeder Pfeil bedeutet: „kennt nur das Interface“.",
        WithoutIntro = "Die Station soll bei jedem Messwert die Anzeige aktualisieren, auf Frost prüfen und loggen. Naheliegend: alles direkt hinschreiben (ausgedachtes Beispiel, so steht es nicht im Projekt).",
        WithoutCode = new("Ohne Pattern (ausgedachtes Beispiel)", PatternSnippets.ObserverWithout),
        Problems =
        [
            "**Enge Kopplung:** Die Station kennt jeden Empfänger und ist fest mit ihnen verdrahtet.",
            "**Jeder neue Empfänger ändert die Station** (z. B. ein SMS-Warner). Dabei kann man Funktionierendes kaputt machen.",
            "**Kein Abmelden zur Laufzeit:** Man bräuchte für jeden Empfänger ein `if (_displayEnabled)`.",
            "**Nicht testbar:** Im Test hängt automatisch eine echte Logdatei dran.",
            "**Ein Fehler zieht alles mit:** Wirft `_frostWarner.Check` eine Exception, kommt `_logger.Write` nie dran.",
        ],
        WithIntro = "Die Station bekommt eine Liste von Zuhörern. Alle erfüllen denselben Vertrag (ein Interface). Die Station kennt nur diesen Vertrag, nicht die konkreten Klassen. Das ist die kleine Stufe-1-Fassung. Die Station schickt den neuen Messwert gleich mit (`Update(reading)`): das nennt man Push-Modell.",
        WithCode =
        [
            new("Der Vertrag", PatternSnippets.ObserverContract),
            new("Das Subject", PatternSnippets.ObserverSubject),
            new("Anmelden", PatternSnippets.ObserverUse),
        ],
        StepsIntro = "Wir folgen einem Messwert (`new WeatherReading(\"00:00\", -1.0, 35)`) durch den Stufe-1-Code.",
        Steps =
        [
            "**Programmstart:** `new Station()` erzeugt eine Station mit leerer Liste `_observers`.",
            "**Anmelden:** `station.Subscribe(display)` legt `display` in die Liste, danach die anderen. `ObserverCount` ist 4.",
            "**Neuer Messwert:** Die Schleife (Konsole oder `SimulationRunner`) holt ihn mit `sensor.ReadNext()` und ruft `station.SetReading(reading)` auf. Der Sensor kennt die Station nicht.",
            "**Merken:** `SetReading` speichert den Wert in `Current`.",
            "**Benachrichtigen:** `NotifyObservers` macht eine Kopie der Liste und geht sie der Reihe nach durch.",
            "**Update:** Für jeden Eintrag läuft `observer.Update(reading)`. Die Station weiß nicht, ob dahinter ein `ScreenDisplay` oder ein `FrostWarner` steckt.",
            "**Jeder reagiert auf seine Art:** Die Anzeige druckt eine Zeile, der Frostwarner warnt bei -1,0 °C, der Sturmwarner tut bei 35 km/h nichts.",
            "**Abmelden:** `station.Unsubscribe(display)`. Beim nächsten Messwert schweigt die Anzeige.",
            "**Stoppen (Stufe 2):** `station.Stop()` ruft bei allen Observern `StationStopped()` auf und leert die Liste. Ein weiteres `SetReading` wirft danach eine `InvalidOperationException`.",
        ],
        Roles =
        [
            new("Subject", "`Subject<T>` (Basisklasse), `Station`, `WarningService`", "Core/Observer/Subject.cs, Core/Services/Station.cs"),
            new("Observer (Interface)", "`IWeatherObserver<T>` mit `Name`, `Update`, `StationStopped`", "Core/Observer/IWeatherObserver.cs"),
            new("Konkrete Observer", "`WarningHistory`, `ReadingHistory`, `WeatherStatistics`, `ActionObserver<T>`, `FilterObserver<T>`, Blazor-Seiten", "Core/Observers/, Core/Observer/, Web/Components/Pages/"),
            new("Anmelden / Abmelden", "`Subscribe(observer)` / `Unsubscribe(observer)`", "Core/Observer/Subject.cs"),
            new("Benachrichtigen", "`NotifyObservers(value)` (protected: nur das Subject selbst)", "Core/Observer/Subject.cs"),
            new("Auslöser", "Die Schleife (Konsole oder `SimulationRunner`): `sensor.ReadNext()`, dann `Station.SetReading(reading)`", "Core/Services/Station.cs"),
            new("Observer UND Subject", "`WarningService` (hört auf Messwerte, meldet Warnungen)", "Core/Services/WarningService.cs"),
        ],
        Pros =
        [
            "**Lose Kopplung:** Das Subject kennt nur ein Interface.",
            "**Erweiterbar:** Neuer Observer = neue Klasse + eine Zeile `Subscribe`.",
            "**Zur Laufzeit änderbar:** An- und Abmelden jederzeit.",
            "**Wiederverwendbar:** Dieselbe Station läuft in Konsole und Web.",
            "**Testbar:** Im Test hängst du einen Fake-Observer an.",
        ],
        Cons =
        [
            "**Schwerer zu verfolgen:** Wer reagiert wann? Der Ablauf ergibt sich aus der Anmeldeliste.",
            "**Speicherlecks** bei vergessenem Abmelden.",
            "**Reihenfolge und Zeitpunkt** der Benachrichtigung sind nicht garantiert.",
            "**Fehler in Observern** bleiben unbemerkt, wenn man sie nicht meldet.",
            "**Bei Threads** braucht man Locks und Vorsicht.",
        ],
        Mistakes =
        [
            "**Vergessenes Abmelden (Speicherleck):** Station und Warn-Dienst leben im `WeatherSystem` (ein DI-Singleton) die ganze Laufzeit und halten eine Referenz auf jede angemeldete Seite. Ohne `Unsubscribe` in `Dispose()` bleibt die Seite für immer im Speicher und bekommt weiter Meldungen. Merke dir den Observer deshalb in einem Feld, denn `Unsubscribe` braucht genau dasselbe Objekt.",
            "**Exception in einem Observer:** Ohne Schutz bekommen alle Observer dahinter nichts mehr. `Subject<T>` fängt Fehler pro Observer ab und meldet sie über das Event `ObserverFailed`. Fehler nie still schlucken.",
            "**Liste ändern, während sie durchlaufen wird:** Meldet sich ein Observer in `Update` selbst ab, knallt ein `foreach` über die Originalliste. Darum läuft die Schleife über eine Kopie (`ToList()`).",
            "**Hintergrund-Thread in Blazor:** Meldungen kommen nicht vom UI-Thread. `StateHasChanged()` darf nur im UI-Kontext laufen, deshalb `InvokeAsync(...)`. Ein `_disposed`-Flag fängt Meldungen ab, die noch „unterwegs“ sind.",
            "**Lange Arbeit in `Update`** bremst alle anderen Observer, denn sie werden nacheinander im selben Thread aufgerufen.",
            "**Endlosschleife:** Löst ein Observer in `Update` wieder `SetReading` aus, dreht sich alles im Kreis.",
        ],
        Quiz =
        [
            new("Warum kennt die Station nur das Interface `IWeatherObserver` und nicht die konkreten Klassen?",
                "Damit sie nicht geändert werden muss, wenn ein neuer Observer dazukommt. Neue Klasse schreiben, Interface umsetzen, `Subscribe` aufrufen. Die Station bleibt unverändert (lose Kopplung)."),
            new("Was passiert, wenn eine Blazor-Seite sich nicht in `Dispose()` abmeldet?",
                "Das Subject (Station bzw. Warn-Dienst, beide leben im DI-Singleton `WeatherSystem` für die ganze Laufzeit) hält weiter eine Referenz auf die Seite. Sie wird nie vom Garbage Collector entfernt (Speicherleck) und wird weiter benachrichtigt, obwohl der Nutzer sie längst verlassen hat. Mit jedem Seitenbesuch kommt ein toter Observer dazu."),
            new("Warum läuft `NotifyObservers` über eine Kopie der Liste?",
                "Ein Observer darf sich in `Update` selbst abmelden. Mit der Originalliste gäbe es eine `InvalidOperationException`, weil die Liste während des `foreach` verändert wird."),
            new("Warum ist `WarningService` Observer und Subject? Was wäre die Alternative?",
                "Er bekommt Messwerte (Observer von `WeatherReading`) und erzeugt daraus Warnungen, die er an eigene Abonnenten weitergibt (Subject von `WeatherWarning`). Alternative: Die Station würde die Regeln selbst prüfen und Warnungen verteilen. Dann hätte sie zwei Aufgaben, und die Regeln wären nicht mehr austauschbar."),
            new("Ein Observer wirft in `Update` eine Exception. Was passiert in Stufe 1, was in Stufe 2?",
                "Stufe 1: Die `foreach`-Schleife bricht ab, alle Observer dahinter bekommen den Messwert nicht, und der Fehler läuft bis zum Aufrufer von `SetReading`. Stufe 2: `Subject<T>` fängt den Fehler pro Observer, meldet ihn über `ObserverFailed` und macht mit dem nächsten Observer weiter."),
            new("Warum braucht man in Blazor `InvokeAsync(StateHasChanged)`, wenn ein Observer eine Meldung bekommt?",
                "Die Meldung kommt aus dem Hintergrund-Thread des `SimulationRunner`. `StateHasChanged` darf in Blazor nur im UI-Kontext der Komponente aufgerufen werden. `InvokeAsync` wechselt dorthin."),
            new("Wie heißen `Update` und `StationStopped` im .NET-Interface `IObserver<T>`?",
                "`Update` heißt dort `OnNext`, und `StationStopped` heißt `OnCompleted`. Zusätzlich gibt es `OnError`."),
        ],
    };

    // ---------------------------------------------------------------
    // Builder
    // ---------------------------------------------------------------

    private static readonly PatternInfo Builder = new()
    {
        Slug = "builder",
        Title = "Builder",
        OneLiner = "Ein komplexes Objekt Schritt für Schritt zusammenbauen und am Ende prüfen.",
        WhenNeeded = "Wenn ein Objekt aus vielen (teils optionalen) Teilen besteht, die richtig verkabelt werden müssen, und ein langer Konstruktor unlesbar würde.",
        DocFile = "docs/patterns/Builder.md",
        Sentence = "Ein Builder sammelt Schritt für Schritt die Wünsche und baut erst am Ende (`Build()`) das komplette, geprüfte Objekt zusammen.",
        AnalogyText = "Du bestellst einen Burger: Brötchen, Patty, Käse, ohne Zwiebeln. Niemand ruft in die Küche „Burger(true, false, true, false, ...)“. Du sagst deine Wünsche nacheinander und bekommst am Ende ein fertiges Ergebnis. Genauso beim Auto-Konfigurator: Erst beim Klick auf „Bestellen“ entsteht das Auto, und fehlt etwas, meldet er „Bitte Motor wählen“.",
        Analogy =
        [
            new("Du als Kunde", "Der Code in `Program.cs`"),
            new("Die Bestellung Schritt für Schritt", "`SetName`, `AddFrostWarning`, `KeepReadings`, ..."),
            new("„Bestellen“-Knopf", "`Build()`"),
            new("Das fertige Essen", "`WeatherSystem`"),
            new("„Bitte Motor wählen“", "`InvalidOperationException` mit hilfreicher Meldung"),
        ],
        DiagramCaption = "Die Wünsche gehen in den Builder. Erst `Build()` erzeugt und verkabelt das fertige System.",
        WithoutIntro = "Eine Wetterstation besteht aus vielen Teilen, die richtig verkabelt sein müssen. Ohne Builder gibt es einen riesigen Konstruktor oder Verkabelung von Hand (beides ausgedachte Beispiele, so steht es nicht im Projekt).",
        WithoutCode = new("Ohne Pattern (ausgedachtes Beispiel)", PatternSnippets.BuilderWithout),
        Problems =
        [
            "**Unlesbar:** Was bedeutet das dritte `true`? Welche Zahl ist der Frost-Grenzwert?",
            "**Verkabelung wird vergessen:** Ein fehlendes `Subscribe` ist kein Compilerfehler. Das Programm läuft, nur fehlt etwas.",
            "**Doppelter Code:** Konsole, Web und Tests kopieren dieselben 15 Zeilen.",
            "**Keine Prüfung:** Niemand sagt dir, dass du keine Regel angegeben hast. Die Station läuft und warnt nie.",
            "**Konstruktor-Explosion:** Bei 5 optionalen Regeln bräuchte man für jede Kombination einen eigenen Konstruktor.",
        ],
        WithIntro = "Jede `Add...`-Methode merkt sich nur den Wunsch und gibt den Builder selbst zurück (`return this`). So lassen sich die Aufrufe hintereinander schreiben (Fluent Interface). Die Verkabelung steckt einmal in `Build()`. Ehrlich gesagt ist das der **Fluent Builder**, nicht die GoF-Originalform (mit Builder-Interface, mehreren konkreten Buildern und einem Director). Ein Interface und einen Director gibt es hier nicht.",
        WithCode =
        [
            new("Benutzung", PatternSnippets.BuilderUse),
            new("Die Verkabelung in Build()", PatternSnippets.BuilderBuild),
        ],
        StepsIntro = "Wir folgen dem Aufruf aus der Web-App.",
        Steps =
        [
            "**`new WeatherStationBuilder()`** erzeugt einen leeren Builder mit Feldern wie `_name = null` und `_readingCount = 144`.",
            "**`.SetName(...)`** speichert den Namen und gibt `this` zurück.",
            "**`.AddAllWarnings()`** setzt nur Flags (`_addFrost = true` usw.). Es wird noch nichts gebaut.",
            "**`.KeepReadings(144)`** merkt sich, wie viele Messwerte der Verlauf behalten soll.",
            "**`.Build()` prüft:** Ist ein Name gesetzt? Sonst `InvalidOperationException`.",
            "**Regeln erzeugen:** `CreateRules()` liest einmal das Singleton `WarningSettings.Instance` und baut für jedes Flag die passende Regel. Gibt es keine einzige Regel, wirft `Build()` ebenfalls eine `InvalidOperationException`.",
            "**Verkabeln:** Station, WarningService, Verlauf, Statistik werden erzeugt und per `Subscribe` verbunden.",
            "**Ergebnis:** Ein fertiges `WeatherSystem`, um das du dich nicht mehr kümmern musst.",
        ],
        Roles =
        [
            new("Builder", "`WeatherStationBuilder`: sammelt die Wünsche, baut am Ende zusammen", "Core/Building/WeatherStationBuilder.cs"),
            new("Bauschritte", "`SetName`, `AddTemperatureChangeWarning`, `AddFrostWarning`, `AddHeatWarning`, `AddStormWarning`, `AddPressureDropWarning`, `AddAllWarnings`, `AddRule`, `KeepReadings`", "Core/Building/WeatherStationBuilder.cs"),
            new("Product (Produkt)", "`WeatherSystem`: das fertige, verkabelte Objekt", "Core/Building/WeatherSystem.cs"),
            new("Client (Aufrufer)", "Code, der die Bauschritte aufruft und `Build()` auslöst. Einen Director (GoF) gibt es hier nicht, am nächsten kommt ihm `AddAllWarnings()`.", "Web/Program.cs, ConsoleApp/WeatherConsole.cs (`RunSimulation`)"),
        ],
        Pros =
        [
            "**Lesbar:** Der Sinn steht im Methodennamen (`AddFrostWarning()`), nicht in einer Parameterliste.",
            "**Verkabelung an einer Stelle:** Keiner kann sie vergessen.",
            "**Zentrale Prüfung:** `Build()` meldet fehlende Pflichtangaben.",
            "**Wiederverwendbar:** Konsole und Web nutzen dieselbe Klasse mit anderen Wünschen.",
            "**Unabhängig:** Jedes `Build()` liefert ein neues System.",
        ],
        Cons =
        [
            "**Mehr Code:** Eine zusätzliche Klasse mit vielen Methoden.",
            "**Fehler erst zur Laufzeit:** Ein vergessenes `SetName` fällt erst bei `Build()` auf.",
            "**Bei einfachen Objekten übertrieben.**",
            "**`Build()` darf man nicht vergessen.**",
        ],
        Mistakes =
        [
            "**`Build()` vergessen:** `var x = new WeatherStationBuilder().SetName(\"A\").AddAllWarnings();` ergibt einen Builder, kein System.",
            "**Pflichtangaben nicht prüfen:** Ein Builder ohne Prüfung liefert halbfertige Objekte. Prüfe in `Build()`, nicht erst später irgendwo.",
            "**Regeln teilen:** `AddRule` nimmt eine Funktion (`Func<IWarningRule>`) statt einer fertigen Regel. Regeln haben Zustand, und zwei Systeme dürfen sich keine Instanz teilen.",
            "**Alles mit einem Builder bauen:** Bei zwei Parametern ist ein Konstruktor besser. Ein Builder lohnt sich bei vielen optionalen Teilen und komplizierter Verkabelung.",
            "**Mit Factory Method verwechseln:** Der Builder baut ein Objekt aus vielen Teilen. Die Factory Method entscheidet, welche Art Objekt entsteht.",
        ],
        Quiz =
        [
            new("Warum geben die `Add...`-Methoden `this` zurück?",
                "Damit man die Aufrufe hintereinander schreiben kann (Fluent Interface): `.SetName(...).AddAllWarnings().Build()`."),
            new("Was passiert, wenn du `Build()` ohne vorheriges `SetName` aufrufst?",
                "`Build()` wirft eine `InvalidOperationException` mit dem Hinweis, zuerst `SetName(...)` aufzurufen. Der Builder prüft die Pflichtangaben, bevor er zusammenbaut."),
            new("Warum nimmt `AddRule` eine Funktion (`Func<IWarningRule>`) und keine fertige Regel?",
                "Regeln haben einen Zustand (z. B. „Frostwarnung ist aktiv“). Jedes `Build()` soll ein unabhängiges System liefern. Mit einer Funktion bekommt jedes System seine **eigene** neue Regel. Mit einer fertigen Regel würden sich zwei Systeme heimlich eine Regel (und deren Zustand) teilen."),
            new("Wer verkabelt, wer hört wem zu? Wo steht das im Code?",
                "In `WeatherStationBuilder.Build()`: Die Station wird von `WarningService`, `ReadingHistory` und `WeatherStatistics` beobachtet. Der `WarningService` wird von der `WarningHistory` beobachtet."),
            new("Wann würdest du keinen Builder benutzen?",
                "Bei Objekten mit wenigen Pflichtparametern und ohne optionale Teile. Ein `new Station(\"Name\")` braucht keinen Builder. Er lohnt sich bei vielen optionalen Teilen und aufwendiger Verkabelung."),
        ],
    };

    // ---------------------------------------------------------------
    // Factory Method
    // ---------------------------------------------------------------

    private static readonly PatternInfo FactoryMethod = new()
    {
        Slug = "factory-method",
        Title = "Factory Method",
        OneLiner = "Die Unterklasse entscheidet, welches Objekt entsteht.",
        WhenNeeded = "Wenn es mehrere Varianten eines Objekts gibt, jede mit eigenen Daten, und neue Varianten dazukommen sollen, ohne dass der Aufrufer sie kennen muss.",
        DocFile = "docs/patterns/FactoryMethod.md",
        Sentence = "Eine Basisklasse legt den Ablauf fest und ruft dabei eine abstrakte Methode auf. Die Unterklasse überschreibt diese Methode und entscheidet damit, welches Objekt entsteht.",
        AnalogyText = "Eine Pizzeria-Kette hat in allen Filialen denselben Ablauf: Teig ausrollen, belegen, backen, schneiden. Aber was belegt wird, entscheidet jede Filiale selbst. Der Ablauf (die Basisklasse) ist überall gleich, die Pizza (das Produkt) bestimmt die Filiale (die Unterklasse). Du als Kunde isst nur „Pizza“.",
        Analogy =
        [
            new("Gleicher Ablauf in jeder Filiale", "`WeatherScenario.Start(...)`"),
            new("Die Filiale entscheidet, was auf die Pizza kommt", "Die Unterklasse überschreibt `CreateSensor(...)`"),
            new("Die Pizza", "`ISensor` (konkret z. B. `StormSensor`)"),
            new("Du als Kunde isst nur „Pizza“", "Der Aufrufer kennt nur `ISensor`"),
            new("Die Filialen", "`StormScenario`, `FrostNightScenario`, `HeatWaveScenario`, `RandomScenario`"),
        ],
        DiagramCaption = "Der Aufrufer ruft immer dasselbe `Start()` auf. Welcher Sensor entsteht, bestimmt das Szenario.",
        WithoutIntro = "Die Oberfläche soll je nach gewähltem Szenario den passenden Sensor erzeugen. Naheliegend ist ein `switch` (ausgedachtes Beispiel, so steht es nicht im Projekt).",
        WithoutCode = new("Ohne Pattern (ausgedachtes Beispiel)", PatternSnippets.FactoryWithout),
        Problems =
        [
            "**Der Aufrufer kennt alle Sensor-Klassen** und ist eng an sie gekoppelt (die Klassen sind sogar `internal`).",
            "**Streuung:** Das Wissen „welches Szenario gehört zu welchem Sensor“ steht an mehreren Stellen.",
            "**Fehleranfällig:** Ein neuer Fall wird leicht an einer Stelle vergessen. Bei `string`-Schlüsseln warnt der Compiler nicht.",
            "**Daten und Verhalten getrennt:** Name, Beschreibung und Sensor gehören zusammen, stehen aber in getrennten Listen.",
            "**Schwer erweiterbar:** Jede Änderung heißt, funktionierenden Code zu ändern. Besser wäre: neuen Code hinzufügen.",
        ],
        WithIntro = "Jedes Szenario ist eine Klasse und erbt von `WeatherScenario`. Die Basisklasse hat `Start(...)` für alle gleich. Darin ruft sie `CreateSensor(...)` auf, und diese Methode ist abstrakt: Jede Unterklasse muss sie überschreiben.",
        WithCode =
        [
            new("Die Basisklasse", PatternSnippets.FactoryCreator),
            new("Eine Unterklasse und der Aufrufer", PatternSnippets.FactoryConcrete),
        ],
        StepsIntro = "Der Nutzer wählt in der Konsole „Sturmfront zieht auf“.",
        Steps =
        [
            "**Menü:** `AskForScenario()` geht durch `ScenarioCatalog.All` und zeigt `Name` und `Description`. Die Konsole kennt keine Szenario-Klasse beim Namen.",
            "**Auswahl:** Der Nutzer tippt `1`. Zurück kommt ein `WeatherScenario`, zur Laufzeit ein `StormScenario`-Objekt.",
            "**Start:** `RunSimulation` ruft `scenario.Start(startTime)` auf, die Methode der Basisklasse.",
            "**Factory Method:** `Start` ruft `CreateSensor(startTime)`. Weil sie abstrakt ist, läuft die Version von `StormScenario` (Polymorphie).",
            "**Erzeugen:** `StormScenario.CreateSensor` erzeugt `new StormSensor(startTime)` und gibt ihn als `ISensor` zurück.",
            "**Prüfung:** `Start` prüft auf `null` und gibt den Sensor weiter.",
            "**Benutzen:** Die Konsole ruft nur noch `sensor.ReadNext()`. Bei „Frostnacht“ ist ab Schritt 4 alles gleich, nur läuft `FrostNightScenario.CreateSensor`.",
        ],
        Roles =
        [
            new("Product", "`ISensor`: Schnittstelle des erzeugten Objekts", "Core/Scenarios/ISensor.cs"),
            new("ConcreteProduct", "`StormSensor`, `FrostNightSensor`, `HeatWaveSensor`, `RandomSensor` (alle internal; die ersten drei erben von der Hilfsklasse `CycleSensor`, `RandomSensor` implementiert `ISensor` direkt)", "Core/Scenarios/Sensors/"),
            new("Creator", "`WeatherScenario` (abstrakt, mit `Start`)", "Core/Scenarios/WeatherScenario.cs"),
            new("Factory Method", "`protected abstract ISensor CreateSensor(DateTime startTime)`", "Core/Scenarios/WeatherScenario.cs"),
            new("ConcreteCreator", "`StormScenario`, `FrostNightScenario`, `HeatWaveScenario`, `RandomScenario`", "Core/Scenarios/*Scenario.cs"),
            new("Verzeichnis (kein GoF-Teil)", "`ScenarioCatalog`: Menüs werden daraus gebaut", "Core/Scenarios/ScenarioCatalog.cs"),
            new("Benutzer", "`SimulationRunner.ChangeScenario`, `WeatherConsole.RunSimulation`", "Web/Services/SimulationRunner.cs"),
        ],
        Pros =
        [
            "**Erweiterbar ohne Änderung** (Open/Closed-Prinzip): Neues Szenario = neue Klassen + ein Katalogeintrag.",
            "**Lose Kopplung:** Der Aufrufer kennt nur `ISensor`. Die Sensor-Klassen dürfen `internal` bleiben.",
            "**Daten und Verhalten zusammen:** Name, Beschreibung und Sensor stehen in einer Klasse.",
            "**Der Ablauf steht an einer Stelle** und prüft z. B. auf `null`.",
        ],
        Cons =
        [
            "**Mehr Klassen:** Für vier Fälle vier Szenario- und vier Sensor-Klassen.",
            "**Vererbung nötig:** Für jede Variante eine Unterklasse.",
            "**Für wenige, feste Fälle übertrieben:** Ein `switch` wäre kürzer.",
            "**Der Ablauf in der Basisklasse** ist schwerer zu durchschauen als ein direkter Aufruf.",
        ],
        Mistakes =
        [
            "**Verwechslung mit statischen Hilfsmethoden:** `WeatherWarning.Frost(reading)` ist eine statische Hilfsmethode (Static Factory Method), keine Factory Method im Sinne des GoF. Es gibt keine Vererbung und keine Unterklasse, die entscheidet. Erkennungsmerkmal der echten Factory Method: eine überschreibbare Methode.",
            "**`ScenarioCatalog.Find` ist keine Fabrik:** Er sucht nur ein vorhandenes Szenario in einer Liste und erzeugt nichts.",
            "**Mit Abstract Factory verwechseln:** Die Abstract Factory ist ein eigenes Fabrik-Objekt, das ganze Familien zusammengehöriger Objekte erzeugt. Wir erzeugen nur ein Produkt. In .NET ist `DbProviderFactory` (ADO.NET) eine Abstract Factory.",
            "**Klassisches .NET-Beispiel für die Factory Method:** `GetEnumerator()`. Jede Collection entscheidet selbst, welchen Enumerator sie liefert, `foreach` kennt nur `IEnumerator<T>`. Hier steckt die überschreibbare Methode in einem Interface statt in einer abstrakten Basisklasse.",
            "**Katalogeintrag vergessen:** Das neue `ThunderstormScenario` ist geschrieben, aber nicht im `ScenarioCatalog`. Es erscheint nicht im Menü.",
            "**`CreateSensor` von außen aufrufen wollen:** Die Methode ist absichtlich `protected`, von außen kommt man nicht heran. Benutze immer `Start`, damit die `null`-Prüfung läuft.",
        ],
        Quiz =
        [
            new("Welche Methode ist die Factory Method, und wer überschreibt sie?",
                "`protected abstract ISensor CreateSensor(DateTime startTime)` in `WeatherScenario`. Überschrieben wird sie von `StormScenario`, `FrostNightScenario`, `HeatWaveScenario` und `RandomScenario`."),
            new("Wieso kennt die Konsole die Klasse `StormSensor` nicht, und warum ist das gut?",
                "Sie bekommt von `Start()` nur ein `ISensor`. So hängt sie nicht von konkreten Klassen ab. Neue Sensoren können dazukommen, ohne dass die Konsole geändert wird. Die Sensor-Klassen dürfen `internal` bleiben."),
            new("Ist `WeatherWarning.Frost(reading)` eine Factory Method im Sinne des GoF?",
                "Nein. Es ist eine **statische** Hilfsmethode (Static Factory Method) ohne Vererbung und ohne Unterklasse, die entscheidet. Erkennungsmerkmal der echten Factory Method ist eine überschreibbare Methode."),
            new("Was musst du tun, damit ein neues Szenario „Gewitter“ in Konsole und Web im Menü erscheint?",
                "Einen `ThunderstormSensor` und ein `ThunderstormScenario : WeatherScenario` schreiben und das Szenario in `ScenarioCatalog` eintragen. Konsole und Web bleiben unverändert, weil ihr Menü aus `ScenarioCatalog.All` gebaut wird."),
            new("Wann ist ein einfacher `switch` besser als eine Factory Method?",
                "Bei wenigen, festen Varianten ohne eigene Daten oder eigenes Verhalten, die selten erweitert werden. Dann ist der `switch` kürzer und leichter zu lesen."),
            new("Was ist der Unterschied zwischen Factory Method und Builder?",
                "Die Factory Method entscheidet, welche Art Objekt entsteht, über eine Unterklasse. Der Builder baut ein komplexes Objekt aus vielen Teilen schrittweise zusammen."),
        ],
    };

    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    private static readonly PatternInfo Singleton = new()
    {
        Slug = "singleton",
        Title = "Singleton",
        OneLiner = "Von einer Klasse gibt es genau eine Instanz, mit einem globalen Zugriffspunkt.",
        WhenNeeded = "Selten. Wenn wirklich nur eine Instanz sinnvoll ist (z. B. gemeinsame Grenzwerte). Meist ist heute der Dependency-Injection-Container die bessere Wahl.",
        DocFile = "docs/patterns/Singleton.md",
        Sentence = "Von einer Klasse gibt es genau eine Instanz (ein Objekt), und es gibt einen globalen Zugriffspunkt darauf.",
        AnalogyText = "Eure Schule hat ein Sekretariat. Wenn du eine Bescheinigung brauchst, gehst du dorthin. Es wäre Chaos, wenn jede Klasse ein eigenes Sekretariat mit eigenen Regeln hätte. Oder das Klassenbuch: Gäbe es drei Kopien, stünde in jeder etwas anderes, und keiner wüsste, welche stimmt.",
        Analogy =
        [
            new("Das eine Sekretariat / Klassenbuch", "`WarningSettings` (die Grenzwerte)"),
            new("„Geh zum Sekretariat“", "`WarningSettings.Instance`"),
            new("Niemand darf ein zweites Sekretariat eröffnen", "`private WarningSettings()`"),
            new("Alle haben dieselben Öffnungszeiten", "Frost bei 0 °C, Sturm ab 75 km/h, überall gleich"),
        ],
        DiagramCaption = "Egal wer fragt: Alle bekommen dasselbe Objekt. Einen zweiten Weg gibt es nicht.",
        WithoutIntro = "Die Grenzwerte sollen im ganzen Programm dieselben sein. Ohne Pattern gibt es zwei naheliegende Wege, beide mit Haken (ausgedachte Beispiele, so steht es nicht im Projekt).",
        WithoutCode = new("Ohne Pattern (ausgedachtes Beispiel)", PatternSnippets.SingletonWithout),
        Problems =
        [
            "**Weg 1 ist mühsam:** Jede Methode trägt Parameter mit, die sie selbst nicht braucht, nur um sie weiterzureichen.",
            "**Weg 2 führt zu Widersprüchen:** Ändert jemand den Frost-Grenzwert, aber vergisst die Konsolenausgabe, zeigt das Programm „0 °C“ und warnt bei 1 °C.",
            "**Mehrere Instanzen:** Hat jede Seite ihr eigenes `new WarningSettings()`, sieht keine die Änderung der anderen.",
            "**Neue Seite = noch ein Parameter in der Kette** oder noch eine Kopie.",
            "**Verschwendung**, falls die Instanz teuer zu erzeugen ist (Datei, Datenbank).",
        ],
        WithIntro = "Die Klasse sorgt selbst dafür, dass es nur eine Instanz gibt, und bietet einen festen Zugriffspunkt. Drei Bausteine: ein `private` Konstruktor, ein `static Instance` und `Lazy<T>`. Die Klasse ist außerdem `sealed`: Der private Konstruktor verhindert Unterklassen praktisch schon, `sealed` macht die Absicht sofort sichtbar. \"Genau eine Instanz\" gilt immer pro laufendem Programm: Konsole und Web-App haben je ein eigenes Singleton mit anderer `InstanceId`. Zwei Browser-Tabs derselben Web-App teilen sich eine Instanz.",
        WithCode =
        [
            new("Das Singleton", PatternSnippets.SingletonWith),
            new("Benutzen", PatternSnippets.SingletonUse),
        ],
        StepsIntro = "So läuft es in der Web-App ab.",
        Steps =
        [
            "**Programmstart:** Es gibt noch keine Instanz. `LazyInstance` ist angelegt, aber leer.",
            "**Erster Zugriff:** In `Program.cs` steht `_ = WarningSettings.Instance;`. Dabei wird `LazyInstance.Value` abgefragt.",
            "**Erzeugen:** `Lazy<T>` merkt „noch nichts da“ und ruft die Lambda auf. Der private Konstruktor läuft (er darf, die Lambda steht in der Klasse) und setzt `InstanceId` und `CreatedAt`.",
            "**Jeder weitere Zugriff** (z. B. in `WeatherStationBuilder.CreateRules()`): `Lazy<T>` gibt das gespeicherte Objekt zurück. Der Konstruktor läuft nie wieder.",
            "**Zwei Threads gleichzeitig?** `Lazy<T>` stellt sicher, dass die Lambda nur einmal läuft. Der zweite Thread wartet kurz und bekommt dasselbe Objekt. (Standard-Modus von `Lazy<T>`: `ExecutionAndPublication`.)",
            "**Ergebnis:** Der Builder gibt die Werte an `new FrostRule(0.0, 1.0)`. Die Konsole liest in `PrintLimits` dieselbe Instanz, diese Seite zeigt `InstanceId` und `CreatedAt`: überall im selben Programm derselbe Wert.",
        ],
        Roles =
        [
            new("Singleton", "`WarningSettings`: die Klasse mit der einen Instanz", "Core/Settings/WarningSettings.cs"),
            new("Zugriffspunkt", "`WarningSettings.Instance`", "Core/Settings/WarningSettings.cs"),
            new("Privater Konstruktor", "`private WarningSettings()` verhindert weitere Instanzen", "Core/Settings/WarningSettings.cs"),
            new("Lazy-Erzeugung", "`Lazy<WarningSettings>`: beim ersten Zugriff, thread-sicher (Standard-Modus `ExecutionAndPublication`)", "Core/Settings/WarningSettings.cs"),
            new("Benutzer", "`WeatherStationBuilder.CreateRules`, `WeatherConsole.PrintLimits`, diese Seite", "Core/Building/WeatherStationBuilder.cs"),
        ],
        Pros =
        [
            "**Genau eine Wahrheit:** Alle sehen dieselben Werte.",
            "**Bequemer Zugriff** ohne Durchreichen.",
            "**Lazy:** Wird nur erzeugt, wenn gebraucht.",
            "**Thread-sicher** durch `Lazy<T>`.",
        ],
        Cons =
        [
            "**Globaler Zustand:** Von überall erreichbar (bei uns sind die Werte zum Glück nur lesbar).",
            "**Versteckte Abhängigkeit:** Man sieht einer Klasse nicht an, dass sie `Instance` benutzt.",
            "**Schwer testbar:** Ein Test kann keine anderen Werte einsetzen.",
            "**Verführt zum Missbrauch:** „Ich brauche gerade nur eine Instanz“ ist kein Grund.",
            "**Zwei Aufgaben in einer Klasse:** Werte halten und Einzigkeit sichern.",
        ],
        Mistakes =
        [
            "**Naive Variante:** `if (_instance == null) _instance = new ...` ist nicht thread-sicher. Zwei Threads können zwei Instanzen erzeugen. `Lazy<T>` löst das.",
            "**Veränderbare Werte:** Mit `{ get; set; }` kann jede Stelle die Grenzwerte ändern, und niemand weiß, wer es war. Singletons möglichst unveränderlich halten.",
            "**Überall benutzen:** Je mehr Klassen `Instance` direkt aufrufen, desto schlechter testbar. Bei uns liest im Core nur der Builder das Singleton und reicht die Werte per Konstruktor weiter. Die Konsole, `Program.cs` der Web-App und diese Seite lesen es zusätzlich, aber nur zum Anzeigen bzw. für den ersten Zugriff.",
            "**Begriffsfalle „DI-Singleton“:** `services.AddSingleton<...>` ist nur eine Lebensdauer im DI-Container. Die Klasse hat einen öffentlichen Konstruktor, man könnte weitere mit `new` bauen. Bei uns sind `WeatherSystem` und `SimulationRunner` DI-Singletons. Das ist nicht das Singleton-Muster. Die anderen Lebensdauern: `AddTransient` liefert bei jedem Anfordern eine neue Instanz, `AddScoped` eine pro Scope (in Blazor Server pro Verbindung eines Browser-Tabs).",
            "**Nicht alles, was einmal vorkommt, ist ein Singleton.** In echten Projekten nimmt man fast immer das DI-Singleton, weil es sich im Test austauschen lässt.",
        ],
        Quiz =
        [
            new("Wie verhindert `WarningSettings`, dass jemand eine zweite Instanz erzeugt?",
                "Durch den `private` Konstruktor. Niemand außerhalb der Klasse kann `new WarningSettings()` schreiben. Die einzige Instanz liegt in `LazyInstance` und ist über `WarningSettings.Instance` erreichbar."),
            new("Wozu dient `Lazy<T>`? Was wäre ohne passiert?",
                "Es erzeugt die Instanz erst beim ersten Zugriff und garantiert, dass auch bei mehreren Threads nur eine entsteht. Ohne wäre die naive `if (_instance == null)`-Variante nicht thread-sicher."),
            new("Warum bekommt `FrostRule` ihre Grenzwerte im Konstruktor, statt `WarningSettings.Instance` zu lesen?",
                "Damit sie testbar bleibt. Im Test kann man `new FrostRule(0, 1)` mit eigenen Werten bauen, ohne vom globalen Zustand abzuhängen. Außerdem sieht man am Konstruktor, was die Regel braucht."),
            new("Was ist der Unterschied zwischen `WarningSettings` und `services.AddSingleton<SimulationRunner>()`?",
                "`WarningSettings` ist ein klassisches GoF-Singleton: Die Klasse erzwingt selbst eine Instanz. `AddSingleton` ist nur eine Lebensdauer im DI-Container. Den `SimulationRunner` kann man im Test trotzdem mit `new` bauen."),
            new("Nenne zwei Nachteile des Singleton-Musters.",
                "Globaler Zustand (von überall erreichbar und ggf. veränderbar), versteckte Abhängigkeiten, schwere Testbarkeit, die Klasse hat zwei Aufgaben (Fachlogik und Einzigkeit)."),
            new("Woran erkennst du in der Web-App, dass es dieselbe Instanz ist?",
                "An der `InstanceId`: Sie wird bei der Erzeugung vergeben und ist bei jedem Zugriff gleich, solange das Programm läuft. Der Nachweis weiter oben auf dieser Seite zeigt sie. Die Konsole hat als eigenes Programm eine andere `InstanceId`."),
        ],
    };
}
