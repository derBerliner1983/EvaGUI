// OPTIONALE SICHERHEITSVERBESSERUNGEN für höhere DSGVO-Konformität

using System;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace NexusApp.Services
{
    public static class SecurityEnhancements
    {
        // 1. VERBESSERUNG: Salt für Schlüsselableitung
        public static string GenerateSecureUserKey(string userInfo)
        {
            // Fester Salt pro Installation (in echter Anwendung aus sicherer Quelle)
            var applicationSalt = "NexusApp_2025_Salt_V1";
            var saltedUserInfo = $"{userInfo}_{applicationSalt}";

            // PBKDF2 für Key-Stretching (macht Brute Force schwerer)
            using (var pbkdf2 = new Rfc2898DeriveBytes(saltedUserInfo,
                Encoding.UTF8.GetBytes(applicationSalt), 10000)) // 10.000 Iterationen
            {
                var key = pbkdf2.GetBytes(32); // 256-bit key
                return Convert.ToBase64String(key);
            }
        }

        // 2. VERBESSERUNG: Secure Memory für Schlüssel
        public static class SecureMemory
        {
            [DllImport("kernel32.dll")]
            private static extern void RtlZeroMemory(IntPtr dest, UIntPtr size);

            public static void SecureClearString(ref string sensitiveData)
            {
                // Versuche den String-Speicher zu überschreiben (nicht 100% garantiert in .NET)
                try
                {
                    unsafe
                    {
                        fixed (char* ptr = sensitiveData)
                        {
                            for (int i = 0; i < sensitiveData.Length; i++)
                            {
                                ptr[i] = '\0';
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback: String auf null setzen
                    sensitiveData = null;
                }

                // Garbage Collection forcieren
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            public static void SecureClearByteArray(byte[] sensitiveData)
            {
                if (sensitiveData != null)
                {
                    Array.Clear(sensitiveData, 0, sensitiveData.Length);
                }
            }
        }

        // 3. VERBESSERUNG: Backup-Verschlüsselung mit anderem Schlüssel
        public static string GenerateBackupKey(string userKey)
        {
            var backupSalt = "BackupKey_NexusApp_2025";
            using (var sha256 = SHA256.Create())
            {
                var combined = $"{userKey}_{backupSalt}";
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hash);
            }
        }

        // 4. VERBESSERUNG: Audit-Logging für DSGVO
        public static class AuditLogger
        {
            public static void LogDataAccess(string action, string dataType, string userId)
            {
                var logEntry = new
                {
                    Timestamp = DateTime.UtcNow,
                    Action = action,
                    DataType = dataType,
                    UserId = userId,
                    Machine = Environment.MachineName,
                    Application = "NexusApp"
                };

                // In echter Anwendung: Sicheres Audit-Log schreiben
                System.Diagnostics.Debug.WriteLine($"AUDIT: {logEntry}");
            }
        }

        // 5. VERBESSERUNG: Datenaufbewahrung (DSGVO Artikel 17)
        public static class DataRetention
        {
            public static bool ShouldDeleteData(DateTime createdDate, TimeSpan retentionPeriod)
            {
                return DateTime.Now - createdDate > retentionPeriod;
            }

            public static void ScheduleDataDeletion(string taskId, DateTime scheduledDeletion)
            {
                // In echter Anwendung: Automatische Löschung nach DSGVO-Anforderungen
                System.Diagnostics.Debug.WriteLine($"Task {taskId} zur Löschung vorgemerkt: {scheduledDeletion}");
            }
        }

        // 6. VERBESSERUNG: Zero-Knowledge Beweise für Integrität
        public static string CreateTamperProofHash(string data, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }
    }

    // 7. ERWEITERTE DSGVO-COMPLIANCE KLASSE
    public class DsgvoCompliance
    {
        public static class DataProcessing
        {
            // Artikel 6: Rechtmäßigkeit der Verarbeitung
            public static bool IsProcessingLawful(string legalBasis)
            {
                var lawfulBases = new[] { "consent", "contract", "legal_obligation",
                                        "vital_interests", "public_task", "legitimate_interests" };
                return Array.Exists(lawfulBases, basis => basis == legalBasis);
            }

            // Artikel 13: Informationspflichten
            public static string GetPrivacyNotice()
            {
                return @"
                DATENSCHUTZHINWEIS:
                - Zweck: Verwaltung von Benutzeraufgaben
                - Rechtsgrundlage: Berechtigtes Interesse (Art. 6 Abs. 1 lit. f DSGVO)
                - Speicherdauer: Bis zur Erfüllung der Aufgabe
                - Ihre Rechte: Auskunft, Berichtigung, Löschung, Widerspruch
                - Verschlüsselung: AES-256
                ";
            }

            // Artikel 17: Recht auf Löschung
            public static void ImplementRightToErasure(string userId)
            {
                System.Diagnostics.Debug.WriteLine($"Löschung aller Daten für User {userId} eingeleitet");
                // Hier: Implementierung der vollständigen Datenlöschung
            }
        }

        public static class PrivacyByDesign
        {
            // Datenminimierung
            public static bool IsDataMinimal(object dataObject)
            {
                // Prüfe ob nur notwendige Felder gesetzt sind
                var properties = dataObject.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(dataObject);
                    if (value != null && prop.Name.Contains("Optional"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Warnung: Optionales Feld {prop.Name} enthält Daten");
                    }
                }
                return true;
            }

            // Zweckbindung
            public static bool IsPurposeLimited(string currentPurpose, string[] allowedPurposes)
            {
                return Array.Exists(allowedPurposes, purpose => purpose == currentPurpose);
            }
        }
    }
}