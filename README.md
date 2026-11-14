# OrcaSlicer Multi Printer Tabs

Mehrere vollständige Orca-Arbeitsbereiche in einem Fenster: Jeder Tab hat einen eigenen Drucker, ein eigenes Projekt und eine eigene Device-Ansicht. So können zum Beispiel Bambu und Snapmaker U1 gleichzeitig geöffnet bleiben.

**Windows x64 · Vorschau 0.9.0**

## Download

**[Installer herunterladen (.exe)](https://github.com/drc85/OrcaSlicer-Multi/releases/download/multi-tabs-v0.9.0/OrcaMultiPrinterTabs-0.9.0-Setup.exe)**

Alternativ: **[Portables ZIP herunterladen](https://github.com/drc85/OrcaSlicer-Multi/releases/download/multi-tabs-v0.9.0/OrcaMultiPrinterTabs-0.9.0-win-x64.zip)** · [Release und Prüfsummen](https://github.com/drc85/OrcaSlicer-Multi/releases/tag/multi-tabs-v0.9.0)

Die automatisch von GitHub angebotenen „Source code“-Archive sind Quellcode, keine Installationsdateien.

## Installation und erster Start

1. Eine kompatible OrcaSlicer-Nightly muss separat installiert sein. Voraussetzung sind Windows 10/11 x64, .NET Framework 4.8 und Orcas benötigte Python-Plugin-Schnittstelle.
2. Installer herunterladen und ausführen. Er installiert die Tab-Oberfläche für deinen Windows-Benutzer und bietet eine Desktop-Verknüpfung an. Alternativ das gesamte portable ZIP in einen beschreibbaren Ordner entpacken.
3. Projekte speichern und Orca vor der ersten Übernahme seiner Einstellungen schließen.
4. **OrcaSlicer Multi Printer Tabs** starten. Bei der portablen Version die Datei `OrcaSlicerMultiPrinterTabs.exe` öffnen; den Unterordner `plugin` und die `.exe.config` daneben lassen.
5. Deine vorhandene `orca-slicer.exe` auswählen.
6. Deinen Orca-Datenordner auswählen. Er muss die Datei `OrcaSlicer.conf` enthalten. Bei einer normalen Installation ist das meist `%APPDATA%\OrcaSlicer`; bei einer portablen Installation kann es ein anderer Ordner sein.
7. Der erste Tab startet mit einer lokalen Kopie dieser Einstellungen. Drucker auswählen und ein Projekt ganz normal in Orca öffnen. Mit **+** weitere unabhängige Tabs hinzufügen.

Die Original-Einstellungen werden nicht verändert. Jeder neue Tab bekommt eine eigene Kopie derselben Vorlage. Spätere Änderungen innerhalb eines Tabs bleiben in dessen Arbeitsordner und werden nicht automatisch auf andere Tabs übertragen.

## Bedienung

| Element | Funktion |
| --- | --- |
| Ansicht ganz links | 1, 2, 4 oder 8 Arbeitsbereiche gleichzeitig anzeigen |
| Drucker-/Projekt-Tab | Zu diesem Arbeitsbereich wechseln; der aktive Tab leuchtet |
| **+** hinter dem letzten Tab | Neuen unabhängigen Arbeitsbereich öffnen |
| Orcas eigenes **X** | Diesen Arbeitsbereich schließen |
| Orcas Minimieren/Maximieren | Das gemeinsame Fenster minimieren/maximieren |
| **X** ganz oben rechts | Alle Arbeitsbereiche und anschließend das Programm schließen |

Jeder Tab zeigt **Drucker · Projektname**. Ein Stern folgt Orcas Anzeige für ungespeicherte Änderungen. Jeder Tab kann ein anderes Projekt öffnen und einen anderen Drucker auswählen.

Die Aufteilung ist bei zwei Ansichten nebeneinander, bei vier 2×2 und bei acht 4×2. Helle rote Trennlinien grenzen die Bereiche ab. Die obere Leiste ist dunkelrot. Freie Fläche der oberen Leiste ziehen, um das Fenster zu verschieben; doppelklicken zum Maximieren/Wiederherstellen.

Beim Schließen fragt Orca selbst nach ungespeicherten Projekten. Es gibt keine zusätzliche Bestätigung der Tab-Oberfläche. Bei Abbruch in Orca bleibt die Sitzung offen; **+** kann einen laufenden Schließvorgang ebenfalls abbrechen.

## Bambu-Drucker und Plugins

Für den hier verwendeten Bambu-Aufbau die folgenden Erweiterungen separat in einer kompatiblen Orca-Version einrichten, bevor neue Tab-Kopien erstellt werden:

- **[Open Bamboo Networking Plugin](https://cloud.orcaslicer.com/p/51a2b22084da)** für die hier verwendete Bambu-Verbindung. [Quellcode der Networking-Bibliothek](https://github.com/persano/open-bambu-networking-plugin). Die Installations-/Bibliotheksanleitung des Plugins beachten und Orca danach neu starten.
- **[Bambu Filament Manager](https://cloud.orcaslicer.com/app/plugins/plugin-hub/3c5fccb6-d04f-4ab6-9b99-5b3eb7ddacb7)** für die zugehörige Filament-/AMS-Verwaltung. Er ergänzt diesen Aufbau; zum bloßen Senden jedes Druckauftrags ist er nicht grundsätzlich erforderlich.

Der Name des Netzwerk-Plugins lautet **Open Bamboo**. Es ist eine andere Erweiterung als die separate Anwendung **Bambu Connect** von Bambu Lab. Anmeldung und Einrichtung erfolgen lokal nach Anleitung der jeweiligen Entwickler.

**Diese Drittanbieter-Plugins werden nicht mitgeliefert.** Die Veröffentlichung enthält nur die Tab-Oberfläche und ihre eigene kleine lokale Brücke zur Anzeige von Drucker- und Projektnamen.

## Nightly und Funktionsumfang

Die Tab-Oberfläche ist ein **separates Windows-Begleitprogramm**. Sie bettet mehrere vollständige Orca-Prozesse in ein Fenster ein. Das Release enthält keinen neu kompilierten OrcaSlicer und ersetzt deine Orca-Installation nicht. Du wählst selbst die passende Orca-EXE aus.

Lokal erprobt wurde **OrcaSlicer 2.6.0-dev, H3C PR #17273, Build `1babccb8`**. Die Kompatibilität mit jeder neueren Nightly ist nicht garantiert. H3C-, CAD- und Druckerunterstützung stammen aus der ausgewählten Orca-Version. CAD-Komponenten werden durch die Tab-Oberfläche nicht nachinstalliert oder erzwungen.

Die Arbeitskopien erhalten benötigte Python-Pakete, Texturen und entpackte Plugin-Bibliotheken. Anwendungsprotokolle und bestimmte temporäre Daten werden beim Kopieren ausgelassen.

## Daten und Datenschutz

Die Veröffentlichung enthält **keine persönlichen Einstellungen, Konten, Druckerdaten, Projekte oder installierten Drittanbieter-Plugins**.

Erst beim Einrichten auf deinem eigenen Computer wird der von dir ausgewählte Datenordner lokal kopiert. Diese Kopien können deine eigenen Profile, Plugins und Anmeldedaten enthalten und sollten nicht weitergegeben werden. Die Tab-Oberfläche lädt diese Daten nicht hoch. Orca und von dir installierte Plugins haben ihr eigenes Netzwerkverhalten.

Die Standard-Arbeitsordner liegen unter `Dokumente\OrcaMulti\OT\<8-stellige-ID>`. Kurze Pfade sind wichtig, da manche Python-/Texturpakete bei langen Pfaden Probleme bekommen. `settings.json`, `session.json` und `host.log` liegen neben dem Begleitprogramm. Diese Dateien und die Arbeitsordner nicht auf GitHub hochladen.

Die eigene Namens-Brücke schreibt Druckername, Projektpfad, Änderungsstatus und Prozess-ID lokal. Sie sendet keine Druckerbefehle und verwendet kein Netzwerk.

## Bekannte Grenzen

- Jeder Tab benötigt die Ressourcen einer vollständigen Orca-Instanz. Zunächst mit zwei Tabs beginnen; acht können viel RAM/GPU-Leistung benötigen.
- Das kurze separate Orca-Fenster beim Start wird durch ausgeblendeten Splashscreen und Fensterbehandlung reduziert. Ein völlig unsichtbarer Start ist nicht garantiert.
- Bei blockiertem Start in der Taskleiste nach Orcas eigenen Plugin-, Anmelde- oder Fehlermeldungen sehen.
- Die Sitzung merkt sich Arbeitsordner und Ansicht, aber keine vollständige Wiederherstellung ungespeicherter Projekte. Projekte normal speichern.
- Kein automatischer Profilabgleich, kein Verschieben der Tabs und kein automatisches Wiederöffnen von Projekten.
- Vollständige Druck-/Kameratests, gemischte Bildschirm-Skalierung und acht gleichzeitige Drucker sind noch nicht umfassend geprüft.
- Windows-Vorschau ohne digitale Signatur; Windows kann deshalb einen Herausgeberhinweis anzeigen.

## Entfernen

Die installierte Version über die Windows-Einstellungen deinstallieren. Bei der portablen Version nach dem Schließen den entpackten Programmordner entfernen.

Arbeitsdaten unter `Dokumente\OrcaMulti\OT` bleiben absichtlich erhalten. Nur selbst löschen, wenn die darin enthaltenen Einstellungen und Projekte nicht mehr gebraucht werden. Die ursprüngliche Orca-Installation bleibt separat.

## Quellcode und ausführliche technische Anleitung

- **[Branch mit dem Tab-Quellcode](https://github.com/drc85/OrcaSlicer-Multi/tree/multi-printer-tabs)**
- **[Ausführliche englische Anleitung und Build-Befehle](https://github.com/drc85/OrcaSlicer-Multi/blob/multi-printer-tabs/tools/multi-printer-tabs/README.md)**
- [Erfolgreicher Build inklusive Tests und Installer-Erstellung](https://github.com/drc85/OrcaSlicer-Multi/actions/runs/43622105735)
- [Originale OrcaSlicer-Dokumentation](README.upstream.md)

Der Tab-Quellcode liegt im Branch `multi-printer-tabs` unter `tools/multi-printer-tabs`. Dieser `main`-Branch zeigt die Projektanleitung; die geerbten Orca-Quelldateien bleiben die Entwicklungsbasis. Das alleinige Bauen dieser Orca-C++-Quelldateien erzeugt noch keine Tab-Oberfläche.

Die Offline-Tests für die Namens-Brücke und die Erhaltung benötigter Plugin-Dateien bestehen. Die Entwicklung wurde mit KI-Unterstützung durchgeführt. Experimentelles unabhängiges Projekt, ohne offizielle Verbindung zu OrcaSlicer, Bambu Lab oder Snapmaker. Lizenz: [AGPL-3.0](LICENSE.txt).
