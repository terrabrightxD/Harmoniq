# Harmoniq — DJ Audio Analysis

Aktueller Implementierungsstand: CrateScope 0.2 (C#/.NET, Windows).

Diese Version ersetzt die Python-Implementierung durch **C#/.NET 8 und WPF**. Die einzelne EXE enthält die benötigte .NET-Laufzeit. Kein Python, kein separates .NET-Setup, kein Browser und kein Server erforderlich. Windows x64. Die EXE ist nicht signiert. WPF ist Windows-spezifisch; diese Neufassung ist noch keine macOS-Anwendung. Ein macOS-Port bleibt offen.

## Start

`Windows/CrateScope.exe` doppelklicken. Audio hinzufügen oder Dateien auf das Fenster ziehen. Die Analyse läuft lokal im Hintergrund. Ein ausgewählter Track zeigt Waveform, Cue-Vorschläge, Messhinweise und Alternativen. Im unteren Bereich können Key/BPM und bis zu acht Cue-Zeiten geändert werden; Cue-Zeiten mit Semikolon trennen. JSON, CSV und Rekordbox-XML stehen über „Exportieren“ bereit. Export enthält die gesamte Bibliothek und überschreibt keine vorhandene Datei.

Bibliothek: `%LOCALAPPDATA%/CrateScopeNative/library.json`. Speicherung über temporäre Datei und Austausch; bestehende Python-Bibliothek wird nicht verändert/importiert. Original-Key/BPM bleiben bei Korrektur erhalten. Gleiche Pfade werden übersprungen; keine automatische Reanalyse veränderter Dateien. Ordner-Drop importiert direkte Dateien. Unterstützte Formate hängen von NAudio/Windows-Media-Foundation-Codecs ab. WAV ist mit dem beigefügten Bestand getestet. MP3/FLAC/M4A sind decoderabhängig, nicht als vollständig validiert ausgewiesen. Unlesbare Dateien liefern pro Track einen Fehler. Laufzeitgrenze: 2 Sekunden bis 30 Minuten.

## Echte Signalverarbeitung — und ihre Grenzen

Die App analysiert dekodierte Samples, weder Dateinamen noch Zufallswerte. Der neue C#-Analysekern berechnet eine 4096-Punkt-Hann-FFT bei 22050 Hz. Spektrale Spitzen werden mit sub-bin-Interpolation in musikalische Tonhöhenklassen umgerechnet und gegen 24 Dur-/Moll-Profile bewertet. Ein schwaches Profil wird als „Offen“ angezeigt, ein kleiner Abstand zwischen Kandidaten als „Tonart prüfen“. A=440 Hz wird vorausgesetzt.

Für BPM werden positive spektrale Änderungen als Onsets gemessen. Bei regelmäßig wiederkehrenden Impulsen wird das Tempo aus einem Konsens ihrer Zeitabstände berechnet. Bei unregelmäßigen Impulsen folgt normalisierte Autokorrelation als Periodizitätsschätzung. Alternativen bei halbem/doppeltem Tempo bleiben sichtbar. Die Kriterien sind Heuristiken; ein regelmäßiger Puls beweist nicht die richtige metrische Ebene.

Das ist tatsächliche Audioanalyse, aber keine garantierte musikalische Wahrheit. Besonders bei Synkopen, mehreren Instrumenten, modulierender Harmonik und wechselndem Tempo sind Fehler möglich. Kein Nachweis von Mixed-In-Key-Gleichwertigkeit. Die neue Engine wurde noch nicht an unabhängig annotierter echter DJ-Musik geprüft.

Energy E1 bleibt ein eigenes Modell: 35 % RMS-Lautheit, 30 % Onsetdichte, 20 % spektrale Helligkeit, 15 % Tempo, auf 1–10 skaliert. Cues markieren Änderungen in lokalen Tonhöhenprofilen; keine semantisch bestätigten Drops. Die Messgrößen `KeySeparation` und `TempoRegularity` stehen im JSON; sie sind keine kalibrierten Wahrscheinlichkeiten.

## Tatsächliche Tests

- Die **gebaute EXE** analysierte 41 WAV-Dateien; darunter 38 Fälle mit bekannten Sollwerten, 44,1-kHz-Variante, gegenphasiges Stereo und ein Track mit wechselnden Tonarten.
- **38/38 Keys korrekt und 38/38 BPM innerhalb ±1 BPM.** Beim 180-BPM-Fall: **179,97 BPM**. Gilt ausschließlich für diesen einfachen synthetischen Entwicklungsbestand.
- Mittlere Analysezeit in diesem Lauf: rund **0,487 s** pro Datei (24–48 Sekunden Audio). Rohwerte in `test-results.json`, Vergleich in `comparison.csv`.
- Wechselnde Tonarten im Abschnittstest wurden global als **„Offen“** ausgegeben; kein unbegründeter globaler Key.
- WPF-Oberfläche auf Windows tatsächlich gestartet und visuell geprüft. Bibliothek, Waveform und Korrekturfelder sichtbar. Manuelle Änderung von 119,97 auf 60 BPM wurde in der separaten Vorschau-Bibliothek gespeichert, OriginalBpm=119,97 blieb erhalten.
- Reale Musikqualität, native Drag-Geste, alle Decoderformate und Rekordbox-End-to-End-Import sind noch nicht separat abgenommen. Die früheren Python-Unit-Tests gelten nicht automatisch für diese Neuimplementierung.

## Build und Reproduktion ohne Python

.NET 8 SDK installieren, im Quellcodeordner:

```powershell
dotnet publish CrateScope.csproj -c Release -o Windows -p:DebugType=None
./Test.ps1
```

`Test.ps1` führt die EXE direkt gegen die mit GenerateFixtures.ps1 erzeugten WAV-Fixtures aus. Die Fixtures werden mit dem eigenen C#-Generator in tools/FixtureGenerator.cs synthetisch erzeugt; Python wird nicht benötigt. Erwartete Werte: `expected.csv`. Laufzeit- und Analyseberichte werden mit neuem Dateinamen erzeugt.

Für externe Dateien:

```powershell
Start-Process ./Windows/CrateScope.exe -ArgumentList '--analyze', 'neuer-bericht.json', 'track.wav' -Wait
```

Bei Pfaden mit Leerzeichen müssen die Argumente entsprechend gequotet werden. Ausgabe wird ausschließlich neu angelegt. Ein Fehlerbericht enthält erfolgreiche Analysen und Fehler separat; Exitcode 1 bedeutet mindestens einen Dateifehler.

## Architektur, Exporte und Lizenzen

`Engine.cs`: vollständiger eigener Analysecode. `MainWindow.xaml`: Oberfläche. `MainWindow.xaml.cs`: Import, Queue-Fortschritt, Bibliothek, Korrekturen und Export. `App.xaml.cs`: Start und Batch-Testmodus. NAudio 2.2.1 decodiert Audio, MIT-Lizenz in `licenses/`. .NET/WPF werden selbstenthaltend gebündelt; entsprechende Runtime-Hinweise sind ebenfalls beigefügt.

Rekordbox-XML nutzt das [offizielle Austauschformat](https://cdn.rekordbox.com/files/20200410160904/xml_format_list.pdf): Collection, Playlist, Datei-URI, AverageBpm, Tonality und Memory-Cues (Num=-1). Kein Beatgrid und kein Datenbankzugriff. Importkompatibilität bleibt im Zielprogramm zu prüfen. Keine bestehenden DJ-Daten wurden geändert.

Technische Grundlagen: [NAudio](https://github.com/naudio/NAudio), [Microsoft Single-File Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview). Verwendete NAudio-Version ist im Projekt festgeschrieben. Eigener Code übernimmt die MIT-Lizenz des vorigen CrateScope-Projekts.


