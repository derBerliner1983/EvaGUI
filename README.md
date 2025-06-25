Projekt: Nexus (User-Management-Tool)
Nexus ist eine Desktop-Anwendung zur teilautomatisierten Verwaltung von Benutzerkonten und Berechtigungen im Active Directory. Sie wurde entwickelt, um die Prozesse für Eintritte, Austritte und Versetzungen (EVA) bei Labor Berlin zu beschleunigen, Fehler zu reduzieren und die IT-Abteilung zu entlasten.

1. Problemstellung & Anwendungsfall
Aktuell basiert die Verwaltung von Personalmaßnahmen oft auf manuell ausgewerteten Excel-Listen, um die notwendigen IT-Prozesse anzustoßen. Dies ist zeitaufwendig und fehleranfällig.

Beispiel-Datenbasis: Personalmaßnahmen Januar 2025 (Anonymisiert)

Datum

Personalnummer

Mitarbeiter

Abteilung

Maßnahmenart

01.01.2025

12345678

Uservorname1 Usernachname1

ADM S Projektmanagement

Einstellung

01.01.2025

23453543

Uservorname3 Usernachname3

Innovationsmanagement

Austritt

03.01.2025

45678901

Uservorname5 Usernachname5

ADM S Projektmanagement

Versetzung

05.01.2025

87654321

Uservorname6 Usernachname6

Qualitätsmanagement

Austritt

07.01.2025

11223344

Uservorname7 Usernachname7

IT-Abteilung

Einstellung

...

...

...

...

...

Quelle: EVAListe_Januar_2025_final.xlsx

Ziel von Nexus ist es, die Verarbeitung dieser Daten sowie die manuelle Eingabe von Einzelfällen über eine benutzerfreundliche, sichere und effiziente Oberfläche zu ermöglichen.

2. Design- & Funktionskonzept
Das Design folgt dem "Desktop-First"-Ansatz und ist für eine schnelle und übersichtliche Bedienung auf PC-Monitoren optimiert.

2.1. Layout-Philosophie

Das Layout ist in drei primäre Zonen aufgeteilt, um einen klaren Arbeitsfluss zu gewährleisten:

Aufgabenliste (Links): Eine zentrale Liste zeigt alle anstehenden Personalmaßnahmen. Dies ist der Ausgangspunkt jeder Aktion. Ein Zähler zeigt die Gesamtzahl der offenen Aufgaben.

Aktions-Panel (Rechts): Der Hauptarbeitsbereich. Er ist kontextsensitiv und passt sich an die ausgewählte Aufgabe an (Einstellung, Versetzung, Austritt).

Globale Admin-Leiste (Unten): Eine persistente Leiste am unteren Rand des Fensters zur einmaligen Eingabe der Admin-Credentials pro Sitzung. Dies verhindert redundante Eingaben und entkoppelt die Authentifizierung von der eigentlichen Aufgabe.

2.2. Farbpalette & Bedeutung

Die Farbgebung dient der intuitiven Nutzerführung und gibt sofortiges visuelles Feedback über den Status und die Art von Informationen.

Primärfarbe (Himmelblau):

Verwendung: Aktive Elemente, Hervorhebungen, Links.

Bedeutung: Klarheit, Vertrauen, Technologie. Führt den Blick des Nutzers und signalisiert Interaktivität.

Status- & Aktionsfarben:

Grün: Standard- & Vorlagen-Gruppen. Signalisiert "sicher", "Standard", "hinzugefügt".

Blau: Manuell hinzugefügte Gruppen. Signalisiert eine bewusste, nutzerdefinierte Ergänzung.

Rot: Zu entfernende Gruppen. Signalisiert "Achtung", "Löschen", "Gefahr". Wird bei Austritten und Versetzungen verwendet.

Gelb: Versetzungen. Signalisiert eine Änderung oder einen Übergangszustand.

Status-Punkte:

● Grün: Status OK (z.B. AD-Verbindung steht, Credentials gültig).

● Grau/Neutral: Status unbekannt oder keine Eingabe erfolgt.

● Rot: Fehler (z.B. AD nicht erreichbar, falsches Passwort).

2.3. Kernfunktionen

Zentrale Aufgabenliste: Alle Personalmaßnahmen auf einen Blick.

Kontextsensitive Aktions-Panels: Die Oberfläche passt sich der gewählten Aufgabe an.

Interaktive Gruppen-Vorschau:

Gruppen aus Vorlagen werden automatisch geladen.

Manuelle Ergänzung von Gruppen per Enter-Eingabe.

Einzelnes Entfernen von Gruppen (Standard oder manuell) per Klick auf das (x)-Symbol vor der finalen Ausführung.

Zentrale Admin-Authentifizierung: Einmalige Eingabe der Admin-Daten pro Sitzung.

Visuelle Status-Indikatoren: Live-Feedback über die Erreichbarkeit von Systemen (AD) und die Gültigkeit der Anmeldedaten.

Datenimport: Einlesen von Personalmaßnahmen aus einer Excel-Datei.

Manuelle Erfassung: Möglichkeit, einzelne Fälle direkt in der GUI anzulegen.

3. Technisches Konzept
3.1. Technologie-Stack

Sprache: C#

Framework: WPF (Windows Presentation Foundation)

Kern-Integration: System.Management.Automation (zur nativen Ausführung von PowerShell-Skripten)

3.2. Architektur: Dynamische Skript-Anpassung

Der Ansatz basiert auf drei Hauptschritten, um die bestehenden PowerShell-Skripte unverändert zu lassen:

Laden: Das PowerShell-Skript wird zur Laufzeit als reine Textdatei in die Anwendung geladen.

Anpassen im Speicher: Interaktive Befehle (z.B. Get-Credential, Read-Host) werden im Text durch die statischen Werte aus der GUI ersetzt.

Ausführen: Der modifizierte Skript-Text wird in einer gekapselten PowerShell-Instanz ausgeführt und die Ausgaben werden zur Anzeige in der GUI abgefangen.

3.3. Technische Anforderungen

.NET-Framework mit WPF-Unterstützung

NuGet-Paket: System.Management.Automation

PowerShell-Ausführungsumgebung auf dem Client-Rechner

4. Hauptvorteile
✅ Benutzerfreundlichkeit: Intuitive Oberfläche statt manueller Skript-Ausführung.

✅ Prozesssicherheit: Reduzierung von Fehlern durch geführte Eingaben und Automatisierung.

✅ Effizienz: Beschleunigung der wiederkehrenden EVA-Prozesse.

✅ Keine Skript-Anpassung: Bestehende und bewährte PowerShell-Skripte können ohne Änderung weiterverwendet werden.

✅ Zentralisierung: Alle Aufgaben werden an einem Ort gebündelt und abgearbeitet.

✅ Transparenz: Klare Protokollierung und Fehleranzeige direkt in der Anwendung.

