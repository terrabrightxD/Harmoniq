# Harmoniq 0.3 — lokale DJ-Vorbereitung

Windows-x64-Anwendung in C#/.NET/WPF, kein Python zur Laufzeit. Im Programm lautet der Name noch CrateScope.

## Start und Bedienung

Das gesamte Release-ZIP entpacken und `CrateScope.exe` starten. `ffmpeg.exe` und die Lizenzhinweise neben der Anwendung belassen. Die .NET-Laufzeit ist in der Haupt-EXE enthalten.

Dateien über „Musik hinzufügen“ oder Drag-and-drop importieren. Ordner-Drop berücksichtigt bekannte Audioendungen. Track auswählen, Abspielen/Pause benutzen; Stop springt an den Anfang. Waveform oder Positionsregler dienen zum Springen, der rechte Regler zur Lautstärke. „Cue hier“ speichert die aktuelle Abspielposition, ein Klick auf einen Cue springt dorthin. Maximal acht Cues; ausgewählten Cue mit „Cue entfernen“ entfernen. Die Zeitposition folgt dem Ausgabegerät, nicht dem vorauseilenden Decoder. Key/BPM/Cue-Zeiten können weiterhin manuell korrigiert werden.

Originaldateien bleiben unverändert. Die separate Bibliothek liegt unter `%LOCALAPPDATA%/CrateScopeNative/library.json`. Exporte JSON/CSV/Rekordbox-XML überschreiben keine existierenden Dateien. Rekordbox-Import weiterhin nicht end-to-end abgenommen. Bestehende analysierte Pfade werden übersprungen; bisher keine Reanalyse-Schaltfläche.

## Audioformate

Geprüft: WAV PCM16, PCM24 und Float, FLAC, AIFF, MP3, AAC in M4A und ADTS, ALAC, OGG/Vorbis, Opus und WMA. Weitere FFmpeg-Codecs werden über den Fallback versucht. Keine Garantie für jedes existierende Format, beschädigte Dateien oder DRM. Grenzen: 2 Sekunden bis 30 Minuten; Fallback-Dekodierung maximal 120 Sekunden.

NAudio/Windows decodiert direkt, FFmpeg dient als Fallback. Analyse und Wiedergabe verwenden dieselbe AudioSource. Für nicht direkt lesbare Formate wird eine temporäre Stereo-WAV angelegt und beim Freigeben entfernt. Kein Upload und kein Netzwerkzugriff bei der Audioanalyse. FFmpeg ist eine separate GPLv3-Anwendung; Distribution, Lizenz und Build-/Quellcodeinformationen liegen im Release bei. Quelle: https://www.gyan.dev/ffmpeg/builds/ .

## Erkennung und tatsächliche Tests

Key: Hann-FFT 4096, interpolierte spektrale Spitzen, lineare Magnitudengewichtung und Vergleich von 24 Dur-/Moll-Profilen. BPM: robuste Onset-Schwelle (95. Perzentil statt absolutem Maximum), Zeitabstandskonsens, Autokorrelation mit interpoliertem Maximum und Halb-/Doppeltempoalternativen. Die Interpretation bleibt mehrdeutig. Konfidenzheuristiken sind keine Wahrscheinlichkeiten. Energy E1 ist ein eigenes Modell; Cues sind akustische Vorschläge, keine bestätigten Drops.

- Synthetische Regression: 38/38 bekannte Keys und 38/38 Tempi innerhalb ±1 BPM.
- Formatregression: 12/12 Codec-/Formatfälle erfolgreich decodiert; C-Dur und ca. 120 BPM erhalten.
- Zwei privat bereitgestellte MP3s lokal analysiert. Dabei wurden eine Halbtempo-Mehrdeutigkeit und unsichere bzw. vom Tag abweichende Tonarten festgestellt. Keine behauptete allgemeine Musikgenauigkeit. Privates Audio und Berichte mit Dateipfaden sind nicht im Repository.
- Beide MP3s bestanden den stummen Audioausgabetest: ausgegebene Samples, Pause, Sprung auf 10 Sekunden und anschließender Positionsfortschritt nachgewiesen.
- Neue Player-Oberfläche tatsächlich geöffnet und visuell geprüft. Die weitere interaktive Bedienungsprüfung wurde vom Nutzer mit Escape beendet; vollständige interaktive Cue-Abnahme daher offen.
- Kein macOS-Port, keine Mixed-In-Key-Vergleichswerte. Die vorigen Testergebnisse in test-results.json stammen aus Version 0.2 und sind historische Daten.

## Build und Reproduktion

.NET 8 SDK und PowerShell, im Repository:

```powershell
dotnet publish CrateScope.csproj -c Release -o Windows -p:DebugType=None
./Setup-Decoder.ps1
./Test.ps1
./TestFormats.ps1
```

Der eigene C#-Fixturegenerator erzeugt den synthetischen Bestand ohne Python. Test.ps1 verwendet die gebaute EXE. TestFormats.ps1 erzeugt Codecvarianten eines synthetischen Signals mit FFmpeg. Setup-Decoder.ps1 lädt einen FFmpeg-Release vom Distributor, prüft dessen veröffentlichten SHA256 und legt Lizenzhinweise bei. Eine Internetverbindung wird nur für Installation/Build benötigt.

CLI: `CrateScope.exe --analyze neuer-bericht.json track.wav`. Bei Leerzeichen Argumente korrekt quoten. Reportdateien werden ausschließlich neu angelegt. Erfolgreiche Dateien und Trackfehler werden separat erfasst. `--playback-test neuer-test.json track.wav` testet lokale Audioausgabe stumm; Audiohardware nötig.

GitHub Actions baut die Windows-Version, führt die synthetischen und Formatprüfungen aus und veröffentlicht v0.3.0 nach erfolgreichem Durchlauf. FFmpeg-Version und Prüfsumme im jeweiligen Paket festgehalten. Eigener Code und NAudio: MIT; Laufzeit-/Drittanbieterhinweise siehe licenses.
