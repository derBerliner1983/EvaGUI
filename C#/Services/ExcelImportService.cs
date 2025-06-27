using ExcelDataReader;
using NexusApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NexusApp.Services
{
    public class ExcelImportService
    {
        public ExcelImportService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public List<UserTask> ImportTasksFromFile(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath).ToLower();

            if (fileExtension == ".csv") return ProcessCsv(filePath);
            if (fileExtension == ".xls" || fileExtension == ".xlsx") return ProcessExcel(filePath);

            throw new NotSupportedException("Nicht unterstütztes Dateiformat. Bitte wählen Sie eine .csv, .xls oder .xlsx Datei.");
        }

        private List<UserTask> ProcessExcel(string filePath)
        {
            var importedTasks = new List<UserTask>();
            bool dataSectionStarted = false;

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                while (reader.Read())
                {
                    if (reader.FieldCount < 3) continue;

                    bool isRowDate = false;
                    bool isHeaderRow = false;
                    
                    // Prüfe auf Header-Zeile
                    string firstCellText = reader.GetValue(0)?.ToString() ?? "";
                    string secondCellText = reader.GetValue(1)?.ToString() ?? "";
                    string thirdCellText = reader.GetValue(2)?.ToString() ?? "";
                    
                    if (firstCellText.Contains("Datum der Maßnahme") || 
                        secondCellText.Contains("Datum der Maßnahme") || 
                        thirdCellText.Contains("Datum der Maßnahme"))
                    {
                        isHeaderRow = true;
                        dataSectionStarted = true;
                        continue; // Header-Zeile überspringen
                    }

                    // KORRIGIERT: Prüfe Spalte B (Index 1) für Datum - das ist normalerweise richtig für Excel
                    object cellValueB = reader.GetValue(1); // Spalte B

                    // Prüft zuerst, ob Excel die Zelle bereits als Datum typisiert hat.
                    if (cellValueB is DateTime)
                    {
                        isRowDate = true;
                    }
                    // Falls nicht, wird versucht, den Textinhalt als Datum zu interpretieren.
                    else if (IsDate(cellValueB?.ToString()))
                    {
                        isRowDate = true;
                    }

                    if (!dataSectionStarted)
                    {
                        if (isRowDate)
                        {
                            dataSectionStarted = true;
                            var task = CreateTaskFromRow(reader);
                            if (task != null) importedTasks.Add(task);
                        }
                    }
                    else
                    {
                        // Nur verarbeiten wenn ein Datum vorhanden ist
                        if (isRowDate)
                        {
                            var task = CreateTaskFromRow(reader);
                            if (task != null) importedTasks.Add(task);
                        }
                    }
                }
            }
            return importedTasks;
        }

        private List<UserTask> ProcessCsv(string filePath)
        {
            var importedTasks = new List<UserTask>();
            var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (!lines.Any()) return importedTasks;

            char delimiter = lines.First().Contains(';') ? ';' : ',';
            bool dataSectionStarted = false;

            foreach (var line in lines)
            {
                var columns = line.Split(delimiter);
                if (columns.Length < 3) continue;

                if (!dataSectionStarted)
                {
                    // KORRIGIERT: Suche nach Header-Zeile mit "Datum der Maßnahme" oder direkt nach Datum in Spalte 1
                    if (line.Contains("Datum der Maßnahme") || 
                        (columns.Length > 1 && IsDate(columns[1])))
                    {
                        dataSectionStarted = true;
                        // Wenn es eine Header-Zeile ist, nicht verarbeiten
                        if (!line.Contains("Datum der Maßnahme"))
                        {
                            var task = CreateTaskFromRow(columns);
                            if (task != null) importedTasks.Add(task);
                        }
                    }
                }
                else
                {
                    // Überspringe Zeilen die nur Semikolons enthalten
                    if (line.Trim() == ";;;;;;;;;" || string.IsNullOrWhiteSpace(line.Replace(";", "")))
                        continue;
                        
                    var task = CreateTaskFromRow(columns);
                    if (task != null) importedTasks.Add(task);
                }
            }
            return importedTasks;
        }

        // Überladene Methode für ExcelDataReader
        private UserTask? CreateTaskFromRow(IExcelDataReader reader)
        {
            var columns = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns[i] = reader.GetValue(i)?.ToString() ?? "";
            }
            return CreateTaskFromRow(columns);
        }

        // KORRIGIERT: Methode für beide CSV und Excel-Daten, angepasst für Ihr Dateiformat
        private UserTask? CreateTaskFromRow(string[] columns)
        {
            try
            {
                // Überspringe Zeilen mit zu wenig Spalten oder leere Zeilen
                if (columns.Length < 7) return null;
                
                // Überspringe Header-Zeilen
                if (columns.Any(c => c.Contains("Datum der Maßnahme") || c.Contains("Personalnummer")))
                    return null;

                // KORRIGIERT: Für Ihre CSV-Datei (basierend auf der bereitgestellten Beispieldatei):
                // Spalte 0: leer (bei CSV)
                // Spalte 1: Datum
                // Spalte 2: Personalnummer  
                // Spalte 3: Mitarbeiter (Name)
                // Spalte 4: Kostenstelle
                // Spalte 5: Abteilung
                // Spalte 6: Vorgesetzter
                // Spalte 7: Maßnahmenart (Einstellung/Versetzung/Austritt)

                // Bestimme welche Spalte das Datum enthält (für unterschiedliche Formate)
                int dateColumn = -1;
                int nameColumn = -1;
                int actionColumn = -1;

                // Suche Datum (normalerweise in Spalte 1 bei CSV, Spalte 0 bei Excel ohne leere erste Spalte)
                for (int i = 0; i < Math.Min(columns.Length, 3); i++)
                {
                    if (IsDate(columns[i]))
                    {
                        dateColumn = i;
                        break;
                    }
                }

                if (dateColumn == -1) return null; // Kein Datum gefunden

                // Basierend auf Datum-Position, bestimme andere Spalten
                if (dateColumn == 0)
                {
                    // Excel-Format oder CSV ohne leere erste Spalte
                    nameColumn = 2;
                    actionColumn = 6;
                }
                else if (dateColumn == 1)
                {
                    // CSV-Format mit leerer erster Spalte (Ihr Format)
                    nameColumn = 3;
                    actionColumn = 7;
                }

                if (nameColumn >= columns.Length || actionColumn >= columns.Length)
                    return null;

                // Prozess-Typ extrahieren
                string processTypeString = columns[actionColumn]?.Trim().Trim('"') ?? "";
                TaskProcessType type;
                switch (processTypeString.ToLower())
                {
                    case "einstellung": type = TaskProcessType.Einstellung; break;
                    case "versetzung": type = TaskProcessType.Versetzung; break;
                    case "austritt": type = TaskProcessType.Austritt; break;
                    case "wiedereinstellung": type = TaskProcessType.Einstellung; break; // Wiedereinstellung als Einstellung behandeln
                    default: 
                        System.Diagnostics.Debug.WriteLine($"Unbekannte Maßnahmenart: '{processTypeString}'");
                        return null;
                }

                // Mitarbeitername extrahieren
                string fullName = columns[nameColumn]?.Trim().Trim('"') ?? "";

                if (string.IsNullOrWhiteSpace(fullName)) return null;

                string id = Guid.NewGuid().ToString();
                return new UserTask(id, fullName, type);
            }
            catch (Exception ex)
            {
                // Debug-Ausgabe für Fehlerdiagnose
                System.Diagnostics.Debug.WriteLine($"Fehler beim Erstellen der Aufgabe: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Spalten: {string.Join(" | ", columns)}");
                return null;
            }
        }

        private bool IsDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            // Bereinigt den String von häufigen Störfaktoren
            var cleanText = text.Trim().Trim('"');

            // Prüft, ob es sich um eine Zahl handelt (Excel OLE Automation Date)
            if (double.TryParse(cleanText, out double oaDate) && oaDate > 0)
            {
                try { DateTime.FromOADate(oaDate); return true; } catch { /* Fällt durch */ }
            }

            // Prüft eine erweiterte Liste von Formaten
            string[] formats = { "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/MM/yyyy", "yyyy-MM-dd", "MMMM dd, yyyy" };
            if (DateTime.TryParseExact(cleanText, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return true;
            }

            // Fallback mit deutscher und invarianter Kultur für mehr Flexibilität
            if (DateTime.TryParse(cleanText, new CultureInfo("de-DE"), DateTimeStyles.None, out _))
            {
                return true;
            }
            if (DateTime.TryParse(cleanText, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return true;
            }

            return false;
        }
    }
}
