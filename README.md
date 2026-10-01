# Wetterstation – Lernprojekt zu Design Patterns

Hallo! Dieses Projekt ist ein Lernbegleiter für dich. Du lernst hier vier **Entwurfsmuster** (englisch: *Design Patterns*) an einem Beispiel kennen, das du sofort anfassen kannst:

| Muster | Kurz gesagt | Schwerpunkt? |
|---|---|---|
| **Observer** | "Sag mir Bescheid, wenn sich etwas ändert." | ja, ausführlich |
| **Builder** | "Baue mir ein komplexes Objekt Schritt für Schritt zusammen." | |
| **Factory Method** | "Die Unterklasse entscheidet, welches Objekt entsteht." | |
| **Singleton** | "Es gibt genau eine Instanz." | |

Ein Entwurfsmuster ist eine bewährte Lösung für ein Problem, das in der Softwareentwicklung immer wieder vorkommt. Es ist kein fertiger Code zum Kopieren, sondern eine **Idee**, die du in deine Klassen übersetzt.

**Die Erklärungen stehen in eigenen Kapiteln unter `docs/`.** Jedes Kapitel zeigt dir erst, wie der Code **ohne** das Muster aussähe und welche Probleme das macht. Dann siehst du die Lösung **mit** Muster und wie die Klassen in diesem Projekt zusammenhängen.

## Inhalt

1. [Lernpfad](#lernpfad)
2. [Worum geht's?](#worum-gehts)
3. [Schnellstart](#schnellstart)
4. [Projektstruktur](#projektstruktur)
5. [Die Patterns](#die-patterns)
6. [Observer-Labor](#observer-labor)
7. [Übungsaufgaben](#übungsaufgaben)
8. [Bewusste Vereinfachungen](#bewusste-vereinfachungen)
9. [Glossar](#glossar)

---

## Lernpfad

Das Projekt hat zwei Stufen. So musst du nicht gleich alles auf einmal verstehen:

| Stufe | Projekt | Inhalt |
|---|---|---|
| **Stufe 1 – Einstieg** | `src/WeatherStation.Beginner` | Ein kleines Konsolenprogramm (ca. 150 bis 200 Zeilen). Nur das Observer-Muster: ein eigenes Interface, eine `List`, keine Threads. |
| **Stufe 2 – Praxis** | `WeatherStation.Core`, `ConsoleApp`, `Web` | Dieselbe Idee, aber "wie im echten Projekt": generisch, Fehlerisolation, Threads, Blazor. Dazu Builder, Factory Method und Singleton. |

Stufe 2 benutzt **dieselben Namen** wie Stufe 1 (`Update`, `Subscribe`, `Unsubscribe`, `SetReading`). Wer Stufe 1 versteht, erkennt alles wieder.

**Empfohlene Reihenfolge:**

1. **Stufe 1 lesen und starten** (`dotnet run --project src/WeatherStation.Beginner`).
2. **[Observer-Kapitel](docs/patterns/Observer.md) lesen.** Es erklärt auch, was in Stufe 2 dazukommt und warum.
3. **[Code-Rundgang "Folge dem Messwert"](docs/CODE-RUNDGANG.md):** Du folgst einem Messwert durch alle Dateien von Stufe 2.
4. **Stufe-2-Konsole starten** und mit den Tasten spielen.
5. Die übrigen Kapitel: [Builder](docs/patterns/Builder.md), [Factory Method](docs/patterns/FactoryMethod.md), [Singleton](docs/patterns/Singleton.md).
6. **Blazor-App und Observer-Labor** ausprobieren.
7. **[Übungsaufgaben](#übungsaufgaben)** lösen.
8. Zum Schluss die **Fortgeschrittenen-Blöcke** im Quelltext nachholen.

> In den Quelltexten von Stufe 2 findest du Kommentarblöcke mit **"FÜR FORTGESCHRITTENE – beim ersten Lesen überspringen"**. Dort geht es um Thread-Sicherheit (`lock`, Copy-on-Write), Blazor-Details (`InvokeAsync`) und Ähnliches. Die Grundidee verstehst du auch ohne sie.

---

## Worum geht's?

Stell dir eine kleine **Wetterstation** vor. Sie misst regelmäßig:

- Temperatur (°C)
- Luftfeuchte (%)
- Luftdruck (hPa)
- Windgeschwindigkeit (km/h)

Aus diesen Messwerten erzeugt das Programm **Warnungen**:

| Warnung | Wann? |
|---|---|
| Temperaturänderung | Die Temperatur hat sich um mindestens 3 °C geändert |
| Frost | Temperatur <= 0 °C |
| Hitze | Temperatur >= 30 °C |
| Sturm | Wind >= 75 km/h |
| Luftdruckabfall | Der Druck fällt um mindestens 3 hPa innerhalb von 3 Stunden |

Damit du nicht auf echtes Wetter warten musst, gibt es **Szenarien**: Sturmfront, Frostnacht, Hitzewelle und Zufallswetter. Ein simulierter Sensor liefert dann passende Messwerte.

### Zwei Oberflächen, ein Kern

- **Konsole** (`WeatherStation.ConsoleApp`): Text, Farben, Tastensteuerung.
- **Web-App mit Blazor** (`WeatherStation.Web`): Diagramme, Tabellen, Benachrichtigungen im Browser.

Beide nutzen **denselben Kern** (`WeatherStation.Core`). Der Kern weiß nichts davon, ob er in einer Konsole oder in einem Browser läuft. Genau das ist der Nutzen der Patterns: Die Station meldet Messwerte, und jede Oberfläche hängt sich einfach dran. Keine Zeile im Kern musste dafür angepasst werden.

---

## Schnellstart

### Voraussetzungen

- **.NET 10 SDK** (`dotnet --version` sollte 10.x anzeigen).
- **Node.js** brauchst du nur, wenn du das Aussehen (CSS) der Web-App ändern willst. Die fertige Datei `src/WeatherStation.Web/wwwroot/css/site.out.css` liegt schon bei. Wenn du `site.css` änderst, baust du sie neu mit `npm install` und `npm run build:css` (siehe `package.json`).

### Stufe 1 starten (Einstieg)

```bash
dotnet run --project src/WeatherStation.Beginner
```

Eine kurze, geführte Geschichte im Terminal: Die Station meldet Messwerte, vier Observer reagieren, und mittendrin meldet sich die Anzeige ab. Lies dazu den Quelltext in `src/WeatherStation.Beginner`.

### Stufe 2: Konsole starten

```bash
dotnet run --project src/WeatherStation.ConsoleApp
```

Du wählst im Menü ein Szenario (Zahl eingeben, `0` = Beenden). Dann kommt alle 700 ms ein Messwert. Während die Simulation läuft:

| Taste | Wirkung |
|---|---|
| `P` | Pause / Weiter |
| `S` | Statistik anzeigen |
| `U` | Messwert-Anzeige **ab**- bzw. wieder **an**melden (`Unsubscribe` / `Subscribe`) |
| `Q` | Zurück zum Menü (die Station wird gestoppt) |

### Stufe 2: Web-App starten

```bash
dotnet run --project src/WeatherStation.Web
```

Die Adresse steht in `src/WeatherStation.Web/Properties/launchSettings.json`:

- Profil `http`: <http://localhost:5239>
- Profil `https`: <https://localhost:7209> (und zusätzlich http://localhost:5239)

Mit `dotnet run` wird das Profil `http` benutzt. Der Browser öffnet sich in der Regel von selbst. Die Seiten:

| Adresse | Seite |
|---|---|
| `/` | Übersicht: Kennzahlen, Szenario wählen, Diagramm, letzte Warnungen |
| `/warnungen` | Alle Warnungen als Tabelle, live |
| `/observer-labor` | Zum Experimentieren mit dem Observer-Muster (siehe [Observer-Labor](#observer-labor)) |
| `/muster` | Übersicht über die Patterns inkl. Singleton-Nachweis und Grenzwerten. Dazu gibt es Erklärseiten je Pattern: `/muster/observer`, `/muster/builder`, `/muster/factory-method`, `/muster/singleton`. Sie fassen die Kapitel aus `docs/` im Browser zusammen. |

### Tests ausführen

```bash
dotnet test
```

Die Tests liegen in `tests/WeatherStation.Tests`. Sie sind auch eine gute Dokumentation: Sie zeigen, wie man die Klassen benutzt.

---

## Projektstruktur

```text
WeatherStation/
├── docs/
│   ├── PLAN.md                      Umsetzungsplan (für Lehrende / Neugierige)
│   ├── CODE-RUNDGANG.md             "Folge dem Messwert": geführte Tour durch den Code
│   └── patterns/                    Ein Kapitel je Pattern
│       ├── Observer.md
│       ├── Builder.md
│       ├── FactoryMethod.md
│       └── Singleton.md
├── src/
│   ├── WeatherStation.Beginner/     STUFE 1 – Einstieg: nur Observer, ohne Threads
│   │   ├── WeatherReading.cs        Messwert (Time, Temperature, WindSpeed)
│   │   ├── IWeatherObserver.cs      Das Observer-Interface mit Update(reading)
│   │   ├── Station.cs               Subject: List, Subscribe, Unsubscribe, SetReading, NotifyObservers
│   │   ├── Observers/               ScreenDisplay, FrostWarner, StormWarner, HighestTemperature
│   │   └── Program.cs               Die geführte Geschichte
│   ├── WeatherStation.Core/         STUFE 2 – DER KERN – keine UI-Abhängigkeit
│   │   ├── Models/                  WeatherReading, WeatherWarning, WarningLevel, WarningType
│   │   ├── Observer/                Observer-Baukasten: IWeatherObserver<T>, Subject<T>,
│   │   │                            ActionObserver<T>, FilterObserver<T>, ObserverError
│   │   ├── Observers/               Fertige Observer: WarningHistory, ReadingHistory, WeatherStatistics
│   │   ├── Services/                Station (Subject) und WarningService (Observer + Subject)
│   │   ├── Rules/                   IWarningRule + FrostRule, HeatRule, StormRule,
│   │   │                            TemperatureChangeRule, PressureDropRule
│   │   ├── Building/                WeatherStationBuilder (Builder) und WeatherSystem (Produkt)
│   │   ├── Scenarios/               WeatherScenario (Factory Method), Storm-/Frost-/HeatWave-/RandomScenario,
│   │   │   │                        ScenarioCatalog, ISensor
│   │   │   └── Sensors/             Die konkreten Sensoren (internal): CycleSensor, StormSensor, ...
│   │   └── Settings/                WarningSettings (Singleton)
│   ├── WeatherStation.ConsoleApp/   Stufe 2: Konsole
│   │   ├── Program.cs               Startpunkt (kurz)
│   │   ├── WeatherConsole.cs        Menü und Simulationsschleife
│   │   └── Observers/               ReadingPrinter, WarningPrinter
│   └── WeatherStation.Web/          Stufe 2: Blazor Server
│       ├── Program.cs               Dienste registrieren (Builder + Singleton)
│       ├── Services/SimulationRunner.cs   Hintergrunddienst: alle 1,5 s ein Messwert
│       └── Components/
│           ├── Pages/               Home, Warnings, ObserverLab, Patterns (+ Erklärseiten je Pattern)
│           └── Shared/WarningToaster.razor   Toast-Benachrichtigungen (Observer mit FilterObserver)
└── tests/WeatherStation.Tests/      xUnit-Tests für den Kern
```

Merke dir diese Regel: **Der Kern (`Core`) kennt keine Oberfläche. Die Oberflächen kennen den Kern.** Die Abhängigkeit zeigt nur in eine Richtung.

---

## Die Patterns

Jedes Kapitel hat denselben Aufbau: Alltagsvergleich, das Problem **ohne** Pattern, die Lösung **mit** Pattern, Schritt-für-Schritt-Ablauf, Rollen und Diagramme, Fundstellen im Projekt, Vor- und Nachteile, typische Fehler, wie es in .NET heißt und ein Quiz zum Selbsttesten.

### Observer (Beobachter) – [Kapitel lesen](docs/patterns/Observer.md)

Wie ein YouTube-Abo: Ein **Subject** (`Station`) führt eine Liste von **Observern** und sagt allen Bescheid, wenn es einen neuen Messwert gibt. Es muss sie dafür nicht kennen. Ein Observer meldet sich mit `Subscribe` an und mit `Unsubscribe` ab. Das Kapitel erklärt auch, was von Stufe 1 zu Stufe 2 dazukommt: `Name`, `StationStopped`, Fehlerisolation, Replay, Verkettung (`WarningService` ist Observer **und** Subject), `FilterObserver`, Thread-Sicherheit und die typischen Blazor-Fallen (Abmelden nicht vergessen, `InvokeAsync`).

### Builder – [Kapitel lesen](docs/patterns/Builder.md)

Wie eine Burger-Bestellung: Du nennst die Wünsche nacheinander (`SetName`, `AddAllWarnings`, `KeepReadings`), und erst `Build()` setzt das fertige, geprüfte `WeatherSystem` zusammen und verkabelt alle Observer. Das Kapitel zeigt, wie der Code mit langem Konstruktor und Verkabelung von Hand aussähe und warum das fehleranfällig ist. Es erklärt auch ehrlich, dass wir die moderne Variante **Fluent Builder** benutzen und worin sie sich von der GoF-Originalform (mit Director) unterscheidet.

### Factory Method – [Kapitel lesen](docs/patterns/FactoryMethod.md)

Wie Pizzeria-Filialen: Der Ablauf (`WeatherScenario.Start`) ist überall gleich, aber die Unterklasse (`StormScenario`, `FrostNightScenario`, ...) entscheidet in `CreateSensor`, welcher Sensor entsteht. Das Kapitel grenzt die Factory Method auch klar ab von statischen Hilfsmethoden wie `WeatherWarning.Frost(...)`, von der Simple Factory und von der Abstract Factory.

### Singleton – [Kapitel lesen](docs/patterns/Singleton.md)

Wie das eine Klassenbuch: `WarningSettings.Instance` liefert überall dasselbe Objekt mit den Grenzwerten. Das Kapitel zeigt die Probleme ohne Singleton, die `Lazy<T>`-Lösung, warum globaler Zustand und Testbarkeit ein Thema sind und wie sich das GoF-Singleton vom DI-`AddSingleton` unterscheidet.

### Alles zusammen: Der Code-Rundgang

Das Kapitel [Folge dem Messwert](docs/CODE-RUNDGANG.md) zeigt, wie die vier Muster zusammenspielen: Der Builder baut das System (und liest das Singleton), die Factory Method erzeugt den Sensor, und der Observer verteilt Messwert und Warnung bis zur Anzeige.

---

## Observer-Labor

Jetzt wird ausprobiert! Die Experimente zeigen dir live, was du im [Observer-Kapitel](docs/patterns/Observer.md) gelesen hast.

### In der Web-App (`/observer-labor`)

Starte die Web-App und öffne den Menüpunkt **Observer-Labor**. Oben stehen die Experimente, darunter zwei Listen ("Beobachter der Station" und die des Warn-Dienstes) und Protokolle. Der Quelltext dazu: `src/WeatherStation.Web/Components/Pages/ObserverLab.razor`.

**Experiment 1: Anmelden und abmelden**

1. Schau dir die Liste **Beobachter der Station** an. Wer steht drin? (Tipp: Die Seite selbst ist auch ein Observer!)
2. Klicke **Test-Observer anmelden**. Er erscheint in der Liste der Warn-Dienst-Beobachter.
3. Wähle oben ein Szenario, z. B. "Sturmfront zieht auf", und warte auf Warnungen. Sie erscheinen im Protokoll.
4. Klicke **Test-Observer abmelden**. Die Warnungen im Protokoll hören auf, und der Name verschwindet aus der Liste.

Was hast du gesehen? `Unsubscribe` hat den Observer aus der Liste entfernt.

**Experiment 2: Fehlerhafter Observer**

1. Klicke **Fehlerhaften Observer anmelden**. Er wirft bei **jedem** Messwert eine Exception.
2. Beobachte: Die Übersicht läuft weiter, die Messwerte kommen an. Im Fehler-Protokoll steht bei jedem Messwert ein Eintrag.
3. Melde ihn wieder ab.

Das ist die **Fehlerisolation**: Ein kaputter Observer bringt die anderen nicht zu Fall, und der Fehler wird über das Event `ObserverFailed` trotzdem sichtbar. Denk an den Nachteil: Was wäre bei einer normalen `foreach`-Schleife ohne `try/catch` passiert?

**Experiment 3: FilterObserver**

1. Schalte den Switch **Nur Gefahr** ein (Beschriftung: "FilterObserver aktiv").
2. Melde den Test-Observer an (oder, wenn er schon läuft: Der Switch meldet ihn mit dem neuen Filter neu an).
3. Wähle "Sturmfront zieht auf". Im Protokoll erscheinen nur noch **Gefahren** (Sturm). Frost, Luftdruck usw. fehlen.
4. Schau in die Beobachterliste: Der Name endet jetzt auf ` (gefiltert)`.

**Experiment 4: Der Toast ist auch nur ein Observer**

Auf jeder Seite poppt bei Warnungen und Gefahren ein Toast auf, aber nicht bei Hinweisen und Entwarnungen. Das macht `WarningToaster.razor` mit einem `FilterObserver`. Suche die Zeile im Code.

**Experiment 5: Seitenwechsel und Aufräumen**

1. Melde den Test-Observer an und wechsle dann auf eine andere Seite. Die Seite meldet ihre Observer in `Dispose()` ab.
2. Kehre zurück. Sind die Test-Observer noch da? (Nein, `Dispose()` in `ObserverLab.razor` hat sie per `Unsubscribe` abgemeldet.)

### In der Konsole (Taste `U`)

1. Starte die Konsole und wähle ein Szenario. Die Messwerte laufen durch.
2. Drücke **`U`**. Die Ausgabe `-- Messwert-Anzeige abgemeldet (Unsubscribe) --` erscheint, die Messwerte verschwinden, **aber die Warnungen kommen weiter**. Die Simulation läuft ja noch. Nur ein Observer wurde abgemeldet.
3. Drücke **`U`** erneut. Die Anzeige ist wieder angemeldet (neues `Subscribe`) und bekommt dank **Replay** **sofort den letzten Messwert**, dann die weiteren.
4. Drücke **`S`** für die Statistik: `WeatherStatistics` hat **alle** Messwerte mitgezählt, auch die, die der Bildschirm nicht angezeigt hat, denn es ist ein anderer Observer.
5. Drücke **`Q`**. Die Station wird gestoppt, und die Observer bekommen `StationStopped` (Ausgabe: `[Messwert-Anzeige] Station beendet`).

Suche die Stelle im Code: `ToggleReadingPrinter` in `WeatherConsole.cs`.

---

## Übungsaufgaben

Die Aufgaben werden schwieriger. Mach zwischendurch `dotnet test`, um zu sehen, dass nichts kaputtgeht. Lösungen gibt es nicht, aber Hinweise zum Aufklappen.

### Aufgabe 1 (sehr leicht, Stufe 1): Neuer Observer "HeatWarner"

Schreibe im Projekt `src/WeatherStation.Beginner` einen neuen Observer `HeatWarner`, der ab **30 °C** eine Hitzewarnung ausgibt. Melde ihn in `Program.cs` mit `Subscribe` an der Station an und starte das Programm.

**Start:** `src/WeatherStation.Beginner/Observers/FrostWarner.cs`.

<details>
<summary>Hinweis</summary>

Kopiere `FrostWarner`, benenne die Klasse um und drehe den Vergleich um (`>=` statt `<=`). Danach fehlt nur noch das `Subscribe` in `Program.cs`. Kommt die Warnung an? Prüfe: Musstest du an der `Station` etwas ändern? (Nein, das ist der Kern des Observer-Musters.)
</details>

### Aufgabe 2 (leicht): Grenzwert ändern

Ändere im Singleton den Hitze-Grenzwert von 30 °C auf 26 °C (und den Entwarnungswert auf 24 °C, sonst wirft `HeatRule` eine Exception). Starte die Hitzewelle und beobachte, was passiert. Passe danach alle Tests an, die jetzt fehlschlagen.

**Start:** `src/WeatherStation.Core/Settings/WarningSettings.cs`, dann `dotnet test`.

<details>
<summary>Hinweis</summary>

Die Regel-Tests (`HysteresisRuleTests.cs`) übergeben ihre Werte selbst und sollten grün bleiben. Schau dir an, welche Tests trotzdem rot werden. Genau da hängt etwas am Singleton. Warum ist das ein Hinweis auf globalen Zustand? (Siehe [Singleton-Kapitel](docs/patterns/Singleton.md).)
</details>

### Aufgabe 3 (leicht bis mittel): Neuer Observer "CSV-Logger"

Schreibe einen Observer `CsvLogger : IWeatherObserver<WeatherReading>`, der jeden Messwert als Zeile in eine Datei schreibt (Zeit;Temperatur;Feuchte;Druck;Wind). Melde ihn in der Konsole an und beim Ende wieder ab. Datei schließen bei `StationStopped` nicht vergessen!

**Start:** Vorbild `src/WeatherStation.ConsoleApp/Observers/ReadingPrinter.cs`, anmelden in `WeatherConsole.RunSimulation`.

<details>
<summary>Hinweis</summary>

Schreibe die Zeile mit `StreamWriter` (bzw. `File.AppendAllText`). Für Zahlen nimm `CultureInfo.InvariantCulture`, sonst steht in der CSV ein Komma mitten in der Zahl. Dein Observer braucht außer `Update` und `StationStopped` auch die Eigenschaft `Name`, damit er im Observer-Labor einen Namen hat.
</details>

### Aufgabe 4 (mittel): Neue Regel "Starkregen / hohe Luftfeuchte"

Schreibe eine Regel `HumidityRule : IWarningRule`, die bei Luftfeuchte >= 95 % warnt und bei <= 85 % entwarnt (Hysterese!). Registriere sie im Builder und prüfe sie mit einem Test.

**Start:** `src/WeatherStation.Core/Rules/FrostRule.cs` als Vorlage kopieren. Registrieren mit `AddRule(() => new HumidityRule(...))` am Builder (in `Program.cs` oder in der Konsole).

<details>
<summary>Hinweis</summary>

Die Regel bekommt ihre Grenzwerte im Konstruktor (nicht aus `WarningSettings.Instance` lesen, siehe Singleton-Kapitel). Für die Meldung brauchst du eine neue statische Methode in `WeatherWarning` und vermutlich einen neuen Wert in `WarningType`. Der Sturm-Sensor erreicht 90 % Luftfeuchte. Welches Szenario würde bei 95 % auslösen, und was musst du dort ändern, um es zu testen? Schreibe erst den Test (Vorbild `HysteresisRuleTests.cs`).
</details>

### Aufgabe 5 (mittel): Neues Szenario "Gewitter" (Factory Method)

Erstelle `ThunderstormScenario` mit passendem `ThunderstormSensor`: Druck fällt schnell, Wind steigt kurz stark an, Temperatur fällt um mehr als 3 °C. Es soll ohne weitere Änderung in Konsole und Web im Menü erscheinen.

**Start:** `StormScenario.cs` und `Sensors/StormSensor.cs` als Vorlage, danach `ScenarioCatalog.cs`.

<details>
<summary>Hinweis</summary>

Du brauchst zwei neue Klassen: einen Sensor (Kurven als Stützpunkte, siehe `CycleSensor.Curve`) und ein Szenario, das `CreateSensor` überschreibt. Vergiss den Eintrag in `ScenarioCatalog` nicht. Alle Werte müssen die Plausibilitätsprüfung in `Station.Validate` bestehen. Wenn du das Muster verstanden hast, musstest du **keine** Zeile in `WeatherConsole.cs` oder `SimulationRunner.cs` ändern. Stimmt das bei dir? Ergänze einen Test in `ScenarioTests.cs`.
</details>

### Aufgabe 6 (mittel): Das Speicherleck finden

Baue absichtlich ein Speicherleck ein und beweise es: Kommentiere in `Warnings.razor` das `Weather.Warnings.Unsubscribe(_observer!);` in `Dispose()` aus. Öffne dann mehrmals die Seite "Warnungen" und wechsle zu anderen Seiten.

Beantworte: Was siehst du im **Observer-Labor** in der Liste "Beobachter des Warn-Dienstes"? Warum wächst sie? Was passiert mit der Anzahl, wenn du die Änderung wieder rückgängig machst?

**Start:** `src/WeatherStation.Web/Components/Pages/Warnings.razor` und `ObserverLab.razor`.

<details>
<summary>Hinweis</summary>

Jede neue Seiteninstanz meldet sich an (`"Warnungen-Tabelle"`), aber keine ab. Die `Station` bzw. der `WarningService` gehören zum `WeatherSystem`, das als DI-Singleton registriert ist (`AddSingleton`, nicht das Singleton-Muster!), und leben, solange die App läuft. Sie halten die Seiten am Leben. Achte auch auf das `_disposed`-Flag: Warum reicht es allein nicht?
</details>

### Aufgabe 7 (anspruchsvoll): Ein Observer meldet sich selbst ab

Schreibe einen Observer, der sich nach dem **dritten** empfangenen Messwert **selbst abmeldet** (in `Update`). Teste ihn: Was geht schief, wenn `Subject<T>` mit einer normalen `List<T>` und `foreach` ohne Kopie arbeiten würde?

**Start:** `src/WeatherStation.Core/Observer/ActionObserver.cs`, Test-Vorbild `tests/WeatherStation.Tests/SubjectTests.cs`.

<details>
<summary>Hinweis</summary>

Der Observer muss die `Station` kennen, um `Unsubscribe(this)` aufzurufen. Schreibe dafür eine eigene Klasse, die die Station im Konstruktor bekommt (mit `ActionObserver` ist das umständlich, weil du den Observer erst nach seiner Erzeugung anmelden kannst). Lies danach nochmal den Abschnitt "Liste ändern, während sie durchlaufen wird" im [Observer-Kapitel](docs/patterns/Observer.md) und die Kommentare in `Subject.NotifyObservers`.
</details>

### Bonus (schwer): Nebenläufigkeit

Starte in einem Test zwei Threads: Einer ruft dauernd `station.SetReading(...)` auf, der andere meldet in einer Schleife tausendfach einen Observer an und wieder ab. Was passiert? Halte die Antworten fest.

Zusatzfrage: Was würde passieren, wenn `WarningService.Update` **ohne** `lock (_ruleLock)` liefe und zwei Threads gleichzeitig Messwerte schicken?

**Start:** `tests/WeatherStation.Tests/SubjectTests.cs` (schau, ob es schon Nebenläufigkeitstests gibt) und `WarningService.cs`.

<details>
<summary>Hinweis</summary>

Nutze `Task.Run` oder `Parallel.For`. Erwartung: Es sollte keine Exception geben, weil Anmelden/Abmelden ein neues Array baut. Bei den Regeln dagegen ist der Zustand (`_isActive`, die Queue in `PressureDropRule`) nicht thread-sicher. Zwei Threads könnten doppelte oder verlorene Warnungen erzeugen oder die Datenstruktur beschädigen.
</details>

---

## Bewusste Vereinfachungen

Damit du dich auf die Patterns konzentrieren kannst, fehlt hier mit Absicht einiges. In einem echten Projekt wäre das anders:

- **Eigene Namen statt .NET-Standard.** Stufe 2 benutzt `IWeatherObserver<T>` mit `Update`/`Subscribe`/`Unsubscribe` statt `IObserver<T>`/`IObservable<T>` mit `OnNext` und einem `IDisposable`-Token. Das ist leichter zu verstehen. Das [Observer-Kapitel](docs/patterns/Observer.md) erklärt, wie die Dinge in .NET heißen.
- **Keine Mehrsprachigkeit.** Texte stehen deutsch direkt im Code und im Markup, es gibt keine `.resx`-Ressourcen.
- **Kein Excel-Export** und überhaupt keine Datei-Exporte in der Web-App.
- **Keine Anmeldung** (keine Benutzer, keine Rechte).
- **Daten nur im Speicher.** Beim Neustart der App sind Verlauf und Statistik weg. Es gibt keine Datenbank.
- **Simulierte Zeit.** Jeder Messwert trägt seine eigene (simulierte) Uhrzeit, ein Schritt sind 10 Minuten. Die Regeln benutzen nur `reading.Time`, nie `DateTime.Now`. So sind sie deterministisch testbar.
- **Ein einziges System in der Web-App.** Alle Browser-Tabs sehen dieselbe Station (DI-Singleton).
- **Grenzwerte sind fest.** `WarningSettings` hat nur Getter und liest nichts aus einer Konfigurationsdatei.

## Glossar

| Begriff | Erklärung |
|---|---|
| **Design Pattern** | Bewährte, wiederverwendbare Lösungsidee für ein häufiges Entwurfsproblem. |
| **GoF** | "Gang of Four": Die vier Autoren des Standardbuchs über Design Patterns. Nach ihnen sind die "klassischen" Muster benannt. |
| **Subject** | Das beobachtete Objekt. Es führt die Liste der Abonnenten. Hier: `Subject<T>`. In .NET heißt es `IObservable<T>`. |
| **Observer** | Objekt, das benachrichtigt werden will. Hier: `IWeatherObserver<T>`. In .NET heißt es `IObserver<T>`. |
| **Subscribe / Unsubscribe** | Anmelden bzw. abmelden eines Observers. |
| **Update** | Die Methode, die ein Observer bei einer neuen Meldung ausführt. In .NET heißt sie `OnNext`. |
| **StationStopped** | Meldung "Es kommt nichts mehr". In .NET heißt sie `OnCompleted`. |
| **Notify** | Alle angemeldeten Observer über eine Änderung informieren (`NotifyObservers`). |
| **Push-Modell** | Das Subject schickt die neuen Daten gleich mit (`Update(reading)`). So arbeitet dieses Projekt. Gegenstück: Pull-Modell, dort holt sich der Observer die Daten selbst ab. |
| **Lose Kopplung** | Klassen kennen sich nur über Interfaces, nicht über konkrete Klassen. So lassen sie sich leicht austauschen. |
| **Interface** | Ein Vertrag: Er legt fest, welche Methoden eine Klasse haben muss, aber nicht, wie sie arbeiten. |
| **Speicherleck** | Objekte bleiben im Speicher, obwohl man sie nicht mehr braucht, weil noch jemand eine Referenz hält (z. B. die Observer-Liste). |
| **Fehlerisolation** | Ein Fehler in einem Observer beeinflusst die anderen nicht. |
| **FilterObserver** | Eine Hülle um einen Observer, die nur passende Werte durchlässt. |
| **Copy-on-Write** | Änderungen erzeugen eine neue Kopie der Liste, statt die alte zu verändern. Wer die alte gerade liest, wird nicht gestört. |
| **Momentaufnahme (Snapshot)** | Kopie des Zustands zu einem Zeitpunkt, hier die Observer-Liste während `NotifyObservers`. |
| **Replay** | Ein neuer Observer bekommt sofort den letzten bekannten Wert. |
| **Hysterese** | Zwei verschiedene Schaltschwellen für "an" und "aus", damit nichts flackert (Frost ab 0 °C, Entwarnung ab 1 °C). |
| **Thread** | Ein Ausführungsstrang im Programm. Mehrere Threads laufen gleichzeitig. |
| **Thread-sicher** | Code, der auch funktioniert, wenn mehrere Threads gleichzeitig darauf zugreifen (hier mit `lock`). |
| **lock** | Sperre: Es darf immer nur ein Thread gleichzeitig in den geschützten Codeblock. |
| **UI-Thread / InvokeAsync** | In Blazor darf nur der UI-Kontext die Oberfläche ändern. `InvokeAsync` wechselt dorthin. |
| **Builder** | Muster: Objekt schrittweise zusammenbauen und am Ende fertig ausliefern. Hier in der Form **Fluent Builder** (eine Klasse, `return this`, `Build()`). |
| **Director** | Beim GoF-Builder eine eigene Klasse, die die Bauschritte in fester Reihenfolge aufruft. Gibt es in diesem Projekt nicht. |
| **Fluent Interface** | Methoden geben `this` zurück, damit man sie hintereinander schreiben kann. |
| **Factory Method** | Muster: Eine überschreibbare (meist abstrakte) Methode in der Basisklasse, die Unterklassen überschreiben, um das erzeugte Objekt zu bestimmen. |
| **Abstract Factory** | Anderes GoF-Muster: Ein Fabrik-Objekt erzeugt eine ganze Familie zusammengehöriger Objekte. Kommt in diesem Projekt nicht vor. |
| **Simple Factory** | Eine Klasse, die (oft per `switch`) entscheidet, welche Klasse sie erzeugt. Kein GoF-Muster. |
| **Statische Fabrikmethode** | Eine `static`-Methode, die ein Objekt bequem erzeugt (z. B. `WeatherWarning.Frost(...)`). Kein GoF-Muster. |
| **Singleton** | Muster: Von einer Klasse gibt es genau eine Instanz (pro laufendem Programm) mit globalem Zugriff. |
| **Lazy&lt;T&gt;** | Erzeugt einen Wert erst beim ersten Zugriff, thread-sicher. |
| **DI (Dependency Injection)** | Ein Container liefert einer Klasse ihre Abhängigkeiten, statt dass sie diese selbst mit `new` erzeugt. |
| **Blazor Server** | Web-Framework von Microsoft: Oberfläche in C# und Razor. Die Logik läuft auf dem Server. |
| **BackgroundService** | Dienst, der im Hintergrund läuft, hier der `SimulationRunner`. |
| **Szenario** | Vorgefertigter Wetterverlauf zum Ausprobieren (Sturm, Frost, Hitze, Zufall). |
| **Plausibilitätsprüfung** | Prüfung, ob ein Wert überhaupt möglich sein kann (z. B. Temperatur -60 bis 60 °C). |

---

Viel Spaß beim Ausprobieren! Wenn dir etwas unklar ist: Öffne die genannte Datei, lies die Kommentare (sie sind extra auf Deutsch geschrieben) und schau in die passenden Tests.
