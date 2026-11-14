# PvP Timeline

[English](README.md) | [简体中文](README.zh-CN.md) | [繁體中文](README.zh-TW.md) | [한국어](README.ko-KR.md) | [Русский](README.ru-RU.md) | [Português (Brasil)](README.pt-BR.md) | [Español](README.es-ES.md) | [ไทย](README.th-TH.md) | [Tiếng Việt](README.vi-VN.md) | [日本語](README.ja-JP.md) | **Deutsch** | [Français](README.fr-FR.md)

Eine persönliche Echtzeit-Aktionszeitleiste für PvP in FINAL FANTASY XIV.

Zeigt Kommandoeinsatz, Zauberzeit, globalen Reaktivierungs-Timer (GCD), Animationssperre, deine eigenen Animationen und Effektrückmeldungen auf einer Zeitleiste. So kannst du nachvollziehen, wann du ein Kommando verwendet hast, wann seine Effekte ankamen und welche Kommandos du dazwischen eingesetzt hast.

![PvP Timeline im Spiel](assets/pvp-timeline-preview.gif)

> Dies ist eine Testversion. Spielaktualisierungen können Beobachtung und Anzeige beeinflussen; bei einzelnen Kommandophasen kann Rückmeldung fehlen. Die Zeitleiste zeigt lokale Beobachtungen und ist keine exakte Rekonstruktion der serverinternen Trefferprüfung. Sie automatisiert keine Kommandos.

## Funktionsweise

![Lokale Beobachtung und Nachrichtenfluss](assets/how-pvp-timeline-works.png)

Das Diagramm trennt die Nachrichten zwischen Spielclient und Server von den lokal beobachteten Zeitdaten. PvP Timeline erfasst angenommene Kommandoanforderungen, lokal sichtbare Zauber- und Reaktivierungszustände, Animationssperren, die Wiedergabe deiner Animationen, empfangene Effektmeldungen und aktuelle Statuseffekt-Schnappschüsse. Die farbigen Spuren entsprechen der Standardanzeige.

Das Plugin sendet keine zusätzlichen Kampfpakete und kann den internen Entscheidungsprozess des Servers nicht einsehen. Ein Tastendruck, aus dem keine Spielanforderung entsteht, ist nicht beobachtbar. Effektrückmeldungen tragen den Zeitpunkt, zu dem dein Client die Nachricht empfing, einschließlich Netzwerk-, Server- und Clientverzögerung.

## Funktionen

- **Getrennte Kommandospuren:** Große Symbole unten zeigen Zauber, Waffenfertigkeiten und bestimmte Sonderkommandos; kleinere Symbole oben zeigen Fähigkeiten in höchstens zwei Zeilen.
- **Getrennte Zeitspuren:** GCD, Zauberzeit, Animationssperre und Animationswiedergabe werden unabhängig erfasst.
- **Effektrückmeldung:** Eine rosa Raute markiert Schaden, Heilung, Statuseffekte oder andere unterstützte Meldungen. Kritische und Direkttreffer lassen das ursprüngliche Symbol dezent leuchten.
- **Verzögerte Effekte:** Unterstützte automatische Detonationen und Folgestufen werden dem ursprünglichen Einsatz zugeordnet oder als durchscheinende Symbole dargestellt, ohne einen weiteren Tastendruck zu erfinden.
- **Eigene Statuseffekte:** Kompakte Historie ausgewählter Verstärkungen und beobachteter Schwächungen.
- **Anpassbare Darstellung:** Durchklick-Sperre, horizontales oder vertikales Layout, umgekehrter Bildlauf, Farben, Hintergrundtransparenz, Symbolgrößen und Gesamtskalierung.
- **Anhalten und prüfen:** Anzeige einfrieren oder aktuelle Aufzeichnungen löschen.

Die Beobachtung ist auf unterstützte Bereiche am **Wolfshöhlen-Pier, in Crystalline Conflict, der PvP-Front und den Stahlschwingen** beschränkt. Gewöhnliche PvE-Gebiete sind ausgeschlossen; neue Karten benötigen eine Aktualisierung der Unterstützungsliste.

## Erste Schritte

### Installation

Benötigt wird eine Umgebung für Plugins der **Dalamud API 15**. Verwende eine zu Spiel und Dalamud passende Version.

1. Installiere und starte XIVLauncher mit aktiviertem Dalamud.
2. Öffne **Dalamud Settings → Experimental**.
3. Füge diese Repository-URL hinzu:

   ```text
   https://raw.githubusercontent.com/chefiffer/dalamud-plugins/main/repo.json
   ```

4. Suche im Plugin-Installer nach **PVPTimeline** und installiere es.
5. Öffne die Einstellungen im Spielchat mit:

   ```text
   /pvptimeline
   ```

6. Betritt den Wolfshöhlen-Pier oder eine unterstützte PvP-Karte und verwende Kommandos.

### Erste Einrichtung

Deaktiviere zunächst **Combat only** und teste an einer Trainingspuppe am Wolfshöhlen-Pier:

1. Entsperre das Overlay unter **Display**, verschiebe es und ändere seine Größe.
2. Passe unter **Layout** Symbolgrößen, Balkenpositionen und -stärken an.
3. Aktiviere danach **Locked**, damit Mausklicks durch das Overlay gehen.

Eine gesperrte Zeitleiste kann nicht direkt angeklickt werden. Öffne die Einstellungen mit `/pvptimeline`; im entsperrten Zustand geht dies auch per Rechtsklick.

## Zeitleiste lesen

### Kommandosymbole und Zeiten

`0s` ist die aktuelle Beobachtungszeit, negative Werte liegen in der Vergangenheit. Raster und Zeitangaben bleiben stehen, während Kommandos und Balken zur Verlaufsseite wandern.

Die untere Spur bedeutet nicht, dass jedes dortige Kommando den GCD verwendet: Auch manche Limitrausch- und Sonderkommandos erscheinen dort. Ein blauer GCD-Balken wird nur bei tatsächlich beobachtetem gemeinsamen Reaktivierungs-Timer gezeichnet.

| Standarddarstellung | Bedeutung |
| --- | --- |
| Blauer Balken | Beobachteter globaler Reaktivierungs-Timer (GCD) |
| Grüner Balken | Lokal beobachtete Zauberzeit |
| Kurzer hellgelber Balken | Animationssperre, getrennt vom GCD |
| Violetter Balken | Beobachtete Wiedergabe deiner Animation |
| Ausgefüllte rosa Raute | Direkte Rückmeldung wie Schaden, Heilung oder Statuseffekt; bei geprüften Bewegungskommandos ggf. Serverbestätigung |
| Leere rosa Raute | Rückmeldung wie visueller Effekt oder Anzeigeänderung; allein kein Beweis für Schaden oder Heilung |
| Durchscheinendes Symbol | Unbestätigte Ausführung oder real ausgelöster Effekt ohne manuellen Einsatz; kein erneuter Tastendruck |
| Symbolleuchten | Kritischer Treffer, Direkttreffer oder kritischer Direkttreffer aus echten Effektmeldungen |
| Dünner grauer Balken (standardmäßig aus) | Eigener Reaktivierungs-Timer des Kommandos, nicht der GCD |

Alle Farben sind anpassbar. Bei Mehrfachzielen können Ergebnisse abweichen; das Symbol fasst beobachtete Trefferkennzeichen zusammen. Ein Leuchten für kritischen Direkttreffer bedeutet nicht, dass alle Ziele so getroffen wurden.

Genesung und Schattenbringer besitzen außerdem **kurze, eigenständige blaue Reaktivierungsbalken** in der Fähigkeitenspur. Sie bleiben eigenständige Reaktivierungen und werden dadurch nicht zu GCD-Kommandos.

### Warum fehlen manchmal violette Balken oder Rauten?

Violette Balken beruhen auf realen Beobachtungen deiner Animationen. Fehlende Beobachtungen, unsichere Zuordnung, Pausen oder Überschreibung können sie auslassen oder verkürzen. Eine Ersetzungsmarke erscheint nur bei bestätigter Überschreibung; ein normales Ende erzeugt keine orange Raute.

Rosa Rauten benötigen echte Effektrückmeldungen. Fehlt die Meldung, ist sie nicht zuverlässig zuzuordnen oder gehört sie zu einer nicht unterstützten Stufe, wird nichts aus der Beschreibung erraten.

**Ein fehlender Balken oder Marker beweist nicht, dass das Kommando fehlgeschlagen ist.** GCD oder Animationssperre werden nicht als Ersatz für Animationswiedergabe verwendet.

## Statuszeilen lesen

Die Zeilen zeigen vor allem **Statuseffekte, die tatsächlich auf deinem Charakter vorhanden sind**. Sie können von dir, Verbündeten oder Gegnern stammen. Der Einsatz eines Kommandos durch einen Verbündeten beweist allein nicht, dass du den Effekt erhalten hast.

Verstärkungen werden mit Start-/Endsymbolen und Verbindungsbalken dargestellt:

- Das Startsymbol bewegt sich mit der Zeit; vor Erreichen des linken Randes wird kein Duplikat erzeugt.
- Am Rand gelangt es in die Warteschlange. Bei bekannter Restzeit erscheint darunter höchstens eine zweistellige Zahl ohne Einheit und ohne Null.
- Die Warteschlange ist nach Endzeit sortiert; früher endende Effekte stehen weiter rechts.
- **Ein Symbol mit kleinem roten Unterstrich ist ein Endpunkt.** Bei bekannter Endzeit kann es schon im Zukunftsbereich erscheinen; der Unterstrich bedeutet nicht, dass der Effekt bereits endete.
- Nach dem Ende bewegt sich der Endpunkt in die Vergangenheit; das Segment verschwindet beim Zusammentreffen mit dem Startsymbol.

Quellen können getrennt erfasst werden. Erneuerungen, Stapel und Entfernungen folgen der tatsächlichen Statusliste; Stapel werden nicht aus der Anzahl verwendeter Kommandos erfunden.

Nicht jeder Effekt wird angezeigt. Das Plugin filtert geprüfte Job-Verstärkungen sowie bestimmte Auswahl-, Bereit- und Folgeeffekte. Diese Liste ist nicht vollständig. Bitte eröffne ein Issue, wenn ein weiterer Effekt angezeigt werden soll.

**Es werden nur bestätigbare Effekte und Countdownwerte dargestellt.** Ohne lesbare Restzeit gibt es keinen Countdown. Bedingungen wie „nur im Kampf wirksam“ werden derzeit nicht getrennt ausgewertet.

## Häufige Einstellungen

| Bereich | Optionen |
| --- | --- |
| Oben | Sprache, Gesamtskalierung, Einfrieren/Fortsetzen, Aufzeichnungen löschen |
| Display | Aktivierung, Durchklick-Sperre, nur im Kampf, in Zwischensequenzen ausblenden, Statuszeilen, native Animationsbeobachtung, Ausrichtung, Hintergründe |
| Layout | Symbolgrößen, Fähigkeitshöhe/-abstand, getrennte Balkenpositionen und -stärken, Zauber-/Animationspositionen |
| Tracks | Sichtbarkeit und Farben der Zeitspuren, kurze eigene Reaktivierungen, Fähigkeiten, Auto-Attacken |
| Time | Pixel pro Sekunde, Verlaufsbereich, Zukunftsraum, Raster, Unterteilungen |
| Feedback | Leuchtfarben, Ersetzungsmarker, Deckkraft ausgelöster Effekte |

Werte können direkt eingegeben oder mit dem Mausrad über dem Feld geändert werden. Die Gesamtskalierung unterstützt **0,2–2,0×**, einschließlich Text.

Die Oberfläche unterstützt vereinfachtes/traditionelles Chinesisch, Englisch, Deutsch, Französisch und Japanisch. Anfangs folgt sie der Dalamud-Sprache; manuelle Auswahl ist möglich. Kommando- und Statusnamen verwenden verfügbare mehrsprachige Ressourcen und können bei Lücken auf Englisch, Clientsprache oder ID zurückfallen. Spielernamen werden nicht übersetzt. `ui-localization.txt` neben der DLL steuert nur UI-Texte und wird nach dem Neuladen angewandt.

## FAQ

### Die Zeitleiste ist nicht sichtbar

Prüfe unterstütztes PvP-Gebiet, Pluginstatus, **Combat only** und **Hide in cutscene**. Unter **Diagnostics** findest du den Beobachtungsstatus. Nach Spielupdates kann eine Anpassung nötig sein.

### Warum erscheint nicht jeder wiederholte Tastendruck?

Gezeigt wird beobachtbarer Kommandoeinsatz, nicht jeder Mausklick. Eingaben ohne Spielanforderung sind unsichtbar; fehlgeschlagene Wiederholungen erzeugen keine Symbolreihe.

### Warum ist automatisch ausgelöster Schaden durchscheinend?

Ein von dir verursachter Effekt kann ohne passenden manuellen Einsatz eintreffen. Automatische Detonationen verursachen realen Schaden ohne erneuten Tastendruck. Sicher zugeordnete Stufen aktualisieren das Ursprungskommando; andere reale Auslöser bleiben ggf. durchscheinend.

### Ist die Rückmeldezeit der Server-Trefferzeitpunkt?

Nein. Sie bezeichnet den Empfang beim Client und enthält Netzwerk-, Server- und Clientverarbeitung. Animationswiedergabe ist ebenfalls kein Server-Schnappschuss; das Ende einer Zauberzeit bestimmt nicht den frühesten Bewegungszeitpunkt.

### Werden Daten hochgeladen oder ganze Partien gespeichert?

Nein, nicht automatisch. Es werden begrenzte aktuelle Daten im Speicher gehalten und beim Gebietswechsel oder Löschen zurückgesetzt. Diagnoseinformationen gelangen nur per Kopierschaltfläche in die lokale Zwischenablage und können Entitäts-, Kommando-/Status-IDs und Zeiten enthalten; prüfe sie vor dem Teilen.

## Grenzen und Hinweise

- Das Zuordnungsfenster pro Kommando beträgt höchstens **14 Sekunden**; überfällige oder mehrdeutige Meldungen werden nicht erzwungen zugeordnet.
- Der Verlauf ist auf 256 Einträge und 120 Sekunden begrenzt; Statuslisten haben ebenfalls Grenzen.
- Animationsbeobachtung konzentriert sich auf den eigenen Charakter und rekonstruiert Zielanimationen, Projektile oder Bodeneffekte nicht vollständig.
- Native Beobachtung nutzt interne Spielschnittstellen. Updates können sie beschädigen oder destabilisieren. Abschalten entfernt nicht zwingend alle Beobachtungshooks.
- Dies ist ein inoffizielles Drittanbieter-Plugin ohne Garantie für Kontosicherheit oder Absturzfreiheit. Beachte die Regeln von Spiel und Plattform.

## Fehler melden

Bitte nenne Pluginversion, Spielregion/-version, Sprache, Job und Kommando, manuelle/automatische Auslösung, Soll-/Istverhalten und Reproduktionsschritte. Füge nach Möglichkeit Screenshot oder kurzes GIF, bei Zuordnungsfehlern kopierte Effektdiagnose und bei Ladefehlern bereinigte Dalamud-Logs bei. Teile keine Token, Passwörter oder irrelevanten privaten Logs.

## Danksagung

Darstellung, Sperrinteraktion und Anpassung wurden von folgenden Projekten inspiriert:

- [ActionTimelineEx](https://github.com/Ted-Jin-Lab/ActionTimelineEx)
- [ActionTimelineReplacement](https://github.com/Loskh/ActionTimelineReplacement)

PvP Timeline ist keine offizielle Version dieser Projekte. Spielsymbole und zugehörige Materialien gehören ihren jeweiligen Rechteinhabern.

## Lizenz

PvP Timeline steht unter der GNU Affero General Public License v3.0 (AGPL-3.0). Siehe [LICENSE](https://github.com/chefiffer/PVPTimeline/blob/main/LICENSE).

Urheberrechts- und Lizenzhinweise des MIT-lizenzierten Upstream-Projekts bleiben in [THIRD-PARTY-NOTICES.txt](https://github.com/chefiffer/PVPTimeline/blob/main/THIRD-PARTY-NOTICES.txt) erhalten.
