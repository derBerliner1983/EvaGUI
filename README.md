# EvaGUI - Grafische Oberfläche für Personalmaßnahmen

## Projektübersicht

Dieses Projekt zielt darauf ab, Personalprozesse bei Labor Berlin zu vereinfachen und zu automatisieren, insbesondere:
- Eintritten
- Austritten 
- Versetzungen (EVA)

Die Lösung bietet eine grafische Benutzeroberfläche (GUI), die bestehende PowerShell-Skripte für Onboarding und Offboarding ansteuert.

## 1. Problemstellung

Aktuell basiert die Verwaltung von Personalmaßnahmen oft auf manuell ausgewerteten Excel-Listen, um IT-Prozesse anzustoßen.

### Personalmaßnahmen Januar 2025 (Anonymisiert)

| Datum | Personalnummer | Mitarbeiter | Kostenstelle | Abteilung | Vorgesetzter | Maßnahmenart | Maßnahmegrund |
|-------|----------------|-------------|--------------|-----------|--------------|--------------|---------------|
| 01.01.2025 | 12345678 | Uservorname1 Usernachname1 | 10002000 | ADM S Projektmanagement | Vorgesetzter1 Nachname1 | Einstellung | - |
| 01.01.2025 | 75435786 | Uservorname2 Usernachname2 | 10002001 | END Stoffwechseldiagnostik | Vorgesetzter2 Nachname2 | Wiedereinstellung | - |
| 01.01.2025 | 23453543 | Uservorname3 Usernachname3 | 10002005 | Innovationsmanagement | Vorgesetzter3 Nachname3 | Austritt | - |
| 02.01.2025 | 98765432 | Uservorname4 Usernachname4 | 10002002 | END Stoffwechseldiagnostik | Vorgesetzter2 Nachname2 | Einstellung | - |
| 03.01.2025 | 45678901 | Uservorname5 Usernachname5 | 10002003 | ADM S Projektmanagement | Vorgesetzter1 Nachname1 | Versetzung | Interne Umstrukturierung |
| 05.01.2025 | 87654321 | Uservorname6 Usernachname6 | 10002004 | Qualitätsmanagement | Vorgesetzter4 Nachname4 | Austritt | Beendigung Probezeit |
| 07.01.2025 | 11223344 | Uservorname7 Usernachname7 | 10002006 | IT-Abteilung | Vorgesetzter5 Nachname5 | Einstellung | - |
| 08.01.2025 | 55667788 | Uservorname8 Usernachname8 | 10002007 | Innovationsmanagement | Vorgesetzter3 Nachname3 | Wiedereinstellung | - |
| 10.01.2025 | 99887766 | Uservorname9 Usernachname9 | 10002008 | Labordiagnostik | Vorgesetzter6 Nachname6 | Einstellung | - |
| 12.01.2025 | 33445566 | Uservorname10 Usernachname10 | 10002009 | ADM S Projektmanagement | Vorgesetzter1 Nachname1 | Austritt | Eigenkündigung |
| 15.01.2025 | 77889900 | Uservorname11 Usernachname11 | 10002010 | END Stoffwechseldiagnostik | Vorgesetzter2 Nachname2 | Versetzung | Abteilungswechsel |
| 18.01.2025 | 22334455 | Uservorname12 Usernachname12 | 10002011 | Qualitätsmanagement | Vorgesetzter4 Nachname4 | Einstellung | - |
| 20.01.2025 | 66778899 | Uservorname13 Usernachname13 | 10002012 | IT-Abteilung | Vorgesetzter5 Nachname5 | Austritt | Befristung ausgelaufen |

*Quelle: EVAListe_Januar_2025_final.xlsx*

### Maßnahmenarten-Legende

- 🟢 **Einstellung**: Neue Mitarbeiter
- 🟡 **Wiedereinstellung**: Rückkehrende Mitarbeiter
- 🔴 **Austritt**: Ausscheidende Mitarbeiter

#### Anonymisierungshinweise
- **Mitarbeiternamen**: zu `Uservorname[X] Usernachname[X]` anonymisiert
- **Vorgesetztennamen**: zu `Vorgesetzter[X] Nachname[X]` anonymisiert
- **Abteilungsbezeichnungen**: zur Lesbarkeit beibehalten

## 2. Technisches Konzept

### 2.1. Technologie

- **Sprache:** C#
- **Framework:** WPF (Windows Presentation Foundation)

**Begründung:** 
- Ideale Wahl für moderne Windows-Desktopanwendungen
- Exzellente GUI-Design-Werkzeuge
- Nahtlose Integration zur Ausführung von PowerShell-Skripten

### 2.2. Kernprinzip: Dynamische Skript-Anpassung

Der Ansatz basiert auf drei Hauptschritten:

1. **Laden:** PowerShell-Skript als Textdatei einlesen
2. **Anpassen im Speicher:** Interaktive Befehle durch statische Werte ersetzen
3. **Ausführen:** Modifizierten Skript-Text in PowerShell-Instanz ausführen

Vorteile:
- Flexibel
- Wartbar
- Keine Änderungen an bestehenden Automatisierungsskripten erforderlich

## 3. Implementierungsdetails

### 3.1. Komponenten

1. **PowerShell-Skript (unverändert):** 
   - Enthält originale Automatisierungslogik
   - Interaktive Eingabeaufforderungen

2. **WPF Benutzeroberfläche:**
   - Formular zur Dateneingabe
   - Dynamische Skript-Ausführung
   - Ausgabe-Logging

3. **C#-Logik:**
   - Skript-Modifikation zur Laufzeit
   - Nahtlose PowerShell-Skript-Ausführung
   - Fehlerbehandlung und Benutzer-Feedback

## Technische Anforderungen

- NuGet-Paket: `System.Management.Automation`
- .NET-Framework mit WPF-Unterstützung
- PowerShell-Ausführungsumgebung

## Hauptvorteile

- ✅ Benutzerfreundliche Oberfläche
- ✅ Automatisierung von Personalmaßnahmen
- ✅ Keine Änderungen an bestehenden Skripten
- ✅ Zentralisierte Prozesssteuerung
- ✅ Fehlerprotokollierung und -behandlung

## Hinweis zur Implementierung

Der Quellcode ist so gestaltet, dass er:
- Sicher arbeitet
- Eingaben validiert
- Fehler abfängt
- Benutzerfreundlich ist

**Wichtig:** Pfade und Konfigurationen müssen an die spezifische Umgebung angepasst werden.
