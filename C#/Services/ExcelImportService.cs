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
                    if (reader.FieldCount < 2) continue;

                    bool isRowDate = false;
                    object cellValueObject = reader.GetValue(1); // Spalte B

                    // Prüft zuerst, ob Excel die Zelle bereits als Datum typisiert hat.
                    if (cellValueObject is DateTime)
                    {
                        isRowDate = true;
                    }
                    // Falls nicht, wird versucht, den Textinhalt als Datum zu interpretieren.
                    else if (IsDate(cellValueObject?.ToString()))
                    {
                        isRowDate = true;
                    }

                    if (!dataSectionStarted)
                    {
                        if (isRowDate)
                        {
                            dataSectionStarted = true;
                            // Erst wenn die Startzeile gefunden wurde, beginnen wir mit der Verarbeitung.
                            var task = CreateTaskFromRow(reader);
                            if (task != null) importedTasks.Add(task);
                        }
                    }
                    else
                    {
                        // Verarbeitet alle nachfolgenden Zeilen.
                        var task = CreateTaskFromRow(reader);
                        if (task != null) importedTasks.Add(task);
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
                if (columns.Length < 2) continue;

                if (!dataSectionStarted)
                {
                    if (IsDate(columns[1]))
                    {
                        dataSectionStarted = true;
                        var task = CreateTaskFromRow(columns);
                        if (task != null) importedTasks.Add(task);
                    }
                }
                else
                {
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

        // Methode für CSV-Daten (string-Array)
        private UserTask? CreateTaskFromRow(string[] columns)
        {
            if (columns.Length < 5) return null;
            try
            {
                // Spalte C (Index 2) für den Prozess-Typ
                string processTypeString = columns[2]?.Trim().Trim('"') ?? "";
                TaskProcessType type;
                switch (processTypeString.ToLower())
                {
                    case "einstellung": type = TaskProcessType.Einstellung; break;
                    case "versetzung": type = TaskProcessType.Versetzung; break;
                    case "austritt": type = TaskProcessType.Austritt; break;
                    default: return null;
                }

                // Spalte D (Index 3) für Nachname, Spalte E (Index 4) für Vorname
                string lastName = columns[3]?.Trim().Trim('"') ?? "";
                string firstName = columns[4]?.Trim().Trim('"') ?? "";
                string fullName = $"{firstName} {lastName}".Trim();

                if (string.IsNullOrWhiteSpace(fullName)) return null;

                string id = Guid.NewGuid().ToString();
                return new UserTask(id, fullName, type);
            }
            catch { return null; }
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
