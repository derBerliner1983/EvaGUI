namespace NexusApp.Models
{
    /// <summary>
    /// Definiert die verschiedenen Arten von Aufgaben, die ausgeführt werden können.
    /// Die Verwendung eines Enums anstelle von Strings verhindert Tippfehler.
    /// </summary>
    public enum TaskProcessType
    {
        Einstellung,
        Versetzung,
        Austritt
    }

    /// <summary>
    /// Repräsentiert eine einzelne Aufgabe, die für einen Benutzer ausgeführt werden soll.
    /// Diese Klasse dient als Datenmodell für die gesamte Anwendung.
    /// </summary>
    public class UserTask
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public TaskProcessType Type { get; set; }

        // KORRIGIERT: Dieser Konstruktor stellt sicher, dass neue Aufgaben korrekt
        // mit allen notwendigen Informationen erstellt werden können.
        public UserTask(string id, string name, TaskProcessType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }
}
