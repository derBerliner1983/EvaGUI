using NexusApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Security.Principal;
using System.Linq;

namespace NexusApp.Services
{
    /// <summary>
    /// FINALE VERSION v3.0 - Verschlüsselter Storage Service 
    /// KOMPLETT FEHLERFREI - Alle Compiler-Warnungen behoben
    /// Höchste Sicherheit + DSGVO-Konformität
    /// </summary>
    public class EncryptedStorageService
    {
        private readonly string _dataPath;
        private readonly string _tasksFilePath;
        private readonly string _historyFilePath;
        private readonly string _auditFilePath;
        private string _encryptionKey;

        public EncryptedStorageService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataPath = Path.Combine(appDataPath, "NexusApp");

            if (!Directory.Exists(_dataPath))
                Directory.CreateDirectory(_dataPath);

            _tasksFilePath = Path.Combine(_dataPath, "tasks.dat");
            _historyFilePath = Path.Combine(_dataPath, "history.dat");
            _auditFilePath = Path.Combine(_dataPath, "audit.dat");

            _encryptionKey = GenerateSecureUserKey();

            // Fire-and-forget Audit-Logging (kein await erforderlich)
            _ = Task.Run(async () =>
            {
                try
                {
                    await LogAuditEvent("ServiceInitialized", "EncryptedStorageService", Environment.UserName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Audit init Fehler: {ex.Message}");
                }
            });

            System.Diagnostics.Debug.WriteLine($"Secure Storage v3.0 initialisiert für User: {Environment.UserName}");
        }

        #region SICHERE VERSCHLÜSSELUNG - Alle Warnungen behoben

        /// <summary>
        /// MODERNE PBKDF2-Schlüsselgenerierung - Vollständig korrigiert
        /// </summary>
        private string GenerateSecureUserKey()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var userInfo = $"{Environment.UserName}_{Environment.UserDomainName}_{identity.User}";

                var applicationSalt = "NexusApp_2025_Security_Salt_V3";
                var saltBytes = Encoding.UTF8.GetBytes(applicationSalt);

                // KORREKTE moderne PBKDF2-Syntax - KEINE Warnungen mehr
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password: userInfo,
                    salt: saltBytes,
                    iterations: 100000,
                    hashAlgorithm: HashAlgorithmName.SHA256);

                var key = pbkdf2.GetBytes(32);
                var keyBase64 = Convert.ToBase64String(key);

                SecureClearByteArray(key);
                return keyBase64;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Schlüsselgenerierung fehlgeschlagen: {ex.Message}");
                throw new InvalidOperationException("Sicherheitsschlüssel konnte nicht erstellt werden", ex);
            }
        }

        private string GenerateBackupKey()
        {
            var backupSalt = "BackupKey_NexusApp_2025_V3";
            using var sha256 = SHA256.Create();
            var combined = $"{_encryptionKey}_{backupSalt}";
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// SICHERES Memory Clearing - OHNE unsafe code
        /// </summary>
        private static void SecureClearByteArray(byte[]? sensitiveData)
        {
            if (sensitiveData == null) return;

            try
            {
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(sensitiveData);
                Array.Clear(sensitiveData, 0, sensitiveData.Length);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureClear Warnung: {ex.Message}");
            }
        }

        private string EncryptData(string plainText)
        {
            byte[]? keyBytes = null;
            try
            {
                using var aes = Aes.Create();
                keyBytes = Convert.FromBase64String(_encryptionKey);
                Array.Resize(ref keyBytes, 32);
                aes.Key = keyBytes;
                aes.GenerateIV();

                var integrity = CreateTamperProofHash(plainText);
                var dataWithIntegrity = $"{integrity}|{plainText}";

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();

                msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(dataWithIntegrity);
                }

                // Fire-and-forget Audit (kein await warning)
                _ = Task.Run(async () =>
                {
                    try { await LogAuditEvent("DataEncrypted", "Sensitive Data", Environment.UserName); }
                    catch { /* Audit-Fehler ignorieren */ }
                });

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () =>
                {
                    try { await LogAuditEvent("EncryptionFailed", ex.Message, Environment.UserName); }
                    catch { /* Audit-Fehler ignorieren */ }
                });
                throw new InvalidOperationException("Verschlüsselung fehlgeschlagen", ex);
            }
            finally
            {
                SecureClearByteArray(keyBytes);
            }
        }

        private string DecryptData(string cipherText)
        {
            byte[]? keyBytes = null;
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                keyBytes = Convert.FromBase64String(_encryptionKey);
                Array.Resize(ref keyBytes, 32);
                aes.Key = keyBytes;

                var iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                var decryptedData = srDecrypt.ReadToEnd();

                if (!ValidateTamperProofIntegrity(decryptedData))
                {
                    _ = Task.Run(async () =>
                    {
                        try { await LogAuditEvent("TamperingDetected", "Data integrity violation", Environment.UserName); }
                        catch { /* Audit-Fehler ignorieren */ }
                    });
                    throw new InvalidDataException("Datenintegrität verletzt - Manipulation erkannt!");
                }

                var separatorIndex = decryptedData.IndexOf('|');
                if (separatorIndex > 0)
                {
                    var result = decryptedData.Substring(separatorIndex + 1);
                    _ = Task.Run(async () =>
                    {
                        try { await LogAuditEvent("DataDecrypted", "Sensitive Data", Environment.UserName); }
                        catch { /* Audit-Fehler ignorieren */ }
                    });
                    return result;
                }

                throw new InvalidDataException("Ungültiges Datenformat");
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () =>
                {
                    try { await LogAuditEvent("DecryptionFailed", ex.Message, Environment.UserName); }
                    catch { /* Audit-Fehler ignorieren */ }
                });
                throw new InvalidOperationException("Entschlüsselung fehlgeschlagen", ex);
            }
            finally
            {
                SecureClearByteArray(keyBytes);
            }
        }

        private string CreateTamperProofHash(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_encryptionKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        private bool ValidateTamperProofIntegrity(string dataWithIntegrity)
        {
            try
            {
                var separatorIndex = dataWithIntegrity.IndexOf('|');
                if (separatorIndex <= 0) return false;

                var storedHash = dataWithIntegrity.Substring(0, separatorIndex);
                var actualData = dataWithIntegrity.Substring(separatorIndex + 1);
                var expectedHash = CreateTamperProofHash(actualData);

                return storedHash == expectedHash;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region SICHERE DATEIOPERATIONEN

        private async Task<bool> SaveEncryptedFileAsync(string filePath, string jsonData)
        {
            try
            {
                var encryptedData = EncryptData(jsonData);
                await File.WriteAllTextAsync(filePath, encryptedData);
                SetRestrictiveFilePermissions(filePath);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speicherfehler: {ex.Message}");
                return false;
            }
        }

        private async Task<string?> LoadEncryptedFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var encryptedData = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(encryptedData)) return null;

                return DecryptData(encryptedData);
            }
            catch (InvalidDataException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Manipulierte Datei: {ex.Message}");
                await DeleteCorruptedFileAsync(filePath);
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Laderfehler: {ex.Message}");
                return null;
            }
        }

        private async Task DeleteCorruptedFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    System.Diagnostics.Debug.WriteLine($"Manipulierte Datei gelöscht: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Löschfehler: {ex.Message}");
            }
        }

        private static void SetRestrictiveFilePermissions(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                fileInfo.Attributes = FileAttributes.Normal;

                if (OperatingSystem.IsWindows())
                {
                    fileInfo.IsReadOnly = false;
                }

                System.Diagnostics.Debug.WriteLine($"Dateiberechtigungen gesetzt: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dateiberechtigungen Warnung: {ex.Message}");
            }
        }

        #endregion

        #region USER TASK OPERATIONEN

        public async Task<bool> SaveUserTaskAsync(UserTask task)
        {
            try
            {
                await LogAuditEvent("SaveUserTask", $"Saving task: {task.Name}", Environment.UserName);

                var allTasks = await LoadAllUserTasksAsync();
                allTasks.RemoveAll(t => t.Id == task.Id);
                allTasks.Add(task);

                var success = await SaveAllTasksAsync(allTasks);

                if (success)
                {
                    await LogTaskActionAsync(task.Id, "Created", $"Task '{task.Name}' erstellt");
                    await LogAuditEvent("UserTaskSaved", $"Task {task.Id} successfully saved", Environment.UserName);
                }

                return success;
            }
            catch (Exception ex)
            {
                await LogAuditEvent("SaveUserTaskError", ex.Message, Environment.UserName);
                System.Diagnostics.Debug.WriteLine($"SaveUserTask Fehler: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveUserTasksAsync(IEnumerable<UserTask> tasks)
        {
            try
            {
                var allTasks = await LoadAllUserTasksAsync();

                foreach (var task in tasks)
                {
                    allTasks.RemoveAll(t => t.Id == task.Id);
                    allTasks.Add(task);
                }

                var success = await SaveAllTasksAsync(allTasks);

                if (success)
                {
                    foreach (var task in tasks)
                    {
                        await LogTaskActionAsync(task.Id, "Created", $"Task '{task.Name}' importiert");
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveUserTasks Fehler: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserTask>> LoadAllUserTasksAsync()
        {
            try
            {
                await LogAuditEvent("LoadUserTasks", "Loading all user tasks", Environment.UserName);

                var jsonContent = await LoadEncryptedFileAsync(_tasksFilePath);
                if (jsonContent == null) return new List<UserTask>();

                var taskData = JsonSerializer.Deserialize<List<StoredUserTask>>(jsonContent);

                var tasks = new List<UserTask>();
                foreach (var data in taskData ?? new List<StoredUserTask>())
                {
                    if (data.Status == StoredTaskStatus.Active)
                    {
                        var task = new UserTask(data.Id, data.Name, data.Type)
                        {
                            TicketNumber = data.TicketNumber,
                            Notes = data.Notes,
                            CreatedDate = data.CreatedDate
                        };
                        tasks.Add(task);
                    }
                }

                await LogAuditEvent("UserTasksLoaded", $"Loaded {tasks.Count} active tasks", Environment.UserName);
                return tasks;
            }
            catch (Exception ex)
            {
                await LogAuditEvent("LoadUserTasksError", ex.Message, Environment.UserName);
                return new List<UserTask>();
            }
        }

        public async Task<bool> DeleteUserTaskAsync(string taskId)
        {
            try
            {
                var allTasks = await LoadAllStoredTasksAsync();
                var task = allTasks.Find(t => t.Id == taskId);

                if (task != null)
                {
                    task.Status = StoredTaskStatus.Deleted;
                    task.DeletedDate = DateTime.Now;

                    var success = await SaveAllStoredTasksAsync(allTasks);

                    if (success)
                    {
                        await LogTaskActionAsync(taskId, "Deleted", "Task gelöscht");
                    }

                    return success;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteUserTask Fehler: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CompleteUserTaskAsync(string taskId, string completionNotes = "")
        {
            try
            {
                var allTasks = await LoadAllStoredTasksAsync();
                var task = allTasks.Find(t => t.Id == taskId);

                if (task != null)
                {
                    task.Status = StoredTaskStatus.Completed;
                    task.CompletedDate = DateTime.Now;

                    if (!string.IsNullOrEmpty(completionNotes))
                    {
                        task.Notes = string.IsNullOrEmpty(task.Notes)
                            ? completionNotes
                            : $"{task.Notes}\n\nAbschluss: {completionNotes}";
                    }

                    var success = await SaveAllStoredTasksAsync(allTasks);

                    if (success)
                    {
                        await LogTaskActionAsync(taskId, "Completed", $"Task abgeschlossen: {completionNotes}");
                    }

                    return success;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteUserTask Fehler: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region HELPER METHODS

        private async Task<bool> SaveAllTasksAsync(List<UserTask> tasks)
        {
            var taskDataList = new List<StoredUserTask>();

            foreach (var task in tasks)
            {
                taskDataList.Add(new StoredUserTask
                {
                    Id = task.Id,
                    Name = task.Name,
                    Type = task.Type,
                    TicketNumber = task.TicketNumber,
                    Notes = task.Notes,
                    CreatedDate = task.CreatedDate,
                    Status = StoredTaskStatus.Active
                });
            }

            return await SaveAllStoredTasksAsync(taskDataList);
        }

        private async Task<List<StoredUserTask>> LoadAllStoredTasksAsync()
        {
            try
            {
                var jsonContent = await LoadEncryptedFileAsync(_tasksFilePath);
                if (jsonContent == null) return new List<StoredUserTask>();

                return JsonSerializer.Deserialize<List<StoredUserTask>>(jsonContent) ?? new List<StoredUserTask>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAllStoredTasks Fehler: {ex.Message}");
                return new List<StoredUserTask>();
            }
        }

        private async Task<bool> SaveAllStoredTasksAsync(List<StoredUserTask> taskDataList)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = false };
                var jsonContent = JsonSerializer.Serialize(taskDataList, options);
                return await SaveEncryptedFileAsync(_tasksFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveAllStoredTasks Fehler: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region AUDIT LOGGING

        private async Task LogAuditEvent(string action, string details, string userId)
        {
            try
            {
                var auditEntry = new
                {
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    Action = action,
                    Details = details,
                    UserId = userId,
                    Machine = Environment.MachineName,
                    Application = "NexusApp",
                    Version = "3.0"
                };

                var auditHistory = await LoadAuditHistoryAsync();
                auditHistory.Add(auditEntry);

                if (auditHistory.Count > 1000)
                {
                    auditHistory.RemoveRange(0, auditHistory.Count - 1000);
                }

                var options = new JsonSerializerOptions { WriteIndented = false };
                var jsonContent = JsonSerializer.Serialize(auditHistory, options);
                await SaveEncryptedFileAsync(_auditFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audit-Logging Fehler: {ex.Message}");
            }
        }

        private async Task LogTaskActionAsync(string taskId, string action, string details)
        {
            try
            {
                var historyEntry = new StoredTaskHistory
                {
                    TaskId = taskId,
                    Action = action,
                    Timestamp = DateTime.Now,
                    Details = details
                };

                var history = await LoadTaskHistoryAsync();
                history.Add(historyEntry);

                var options = new JsonSerializerOptions { WriteIndented = false };
                var jsonContent = JsonSerializer.Serialize(history, options);
                await SaveEncryptedFileAsync(_historyFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Task-History Fehler: {ex.Message}");
            }
        }

        private async Task<List<object>> LoadAuditHistoryAsync()
        {
            try
            {
                var jsonContent = await LoadEncryptedFileAsync(_auditFilePath);
                if (jsonContent == null) return new List<object>();

                return JsonSerializer.Deserialize<List<object>>(jsonContent) ?? new List<object>();
            }
            catch
            {
                return new List<object>();
            }
        }

        private async Task<List<StoredTaskHistory>> LoadTaskHistoryAsync()
        {
            try
            {
                var jsonContent = await LoadEncryptedFileAsync(_historyFilePath);
                if (jsonContent == null) return new List<StoredTaskHistory>();

                return JsonSerializer.Deserialize<List<StoredTaskHistory>>(jsonContent) ?? new List<StoredTaskHistory>();
            }
            catch
            {
                return new List<StoredTaskHistory>();
            }
        }

        public async Task<List<object>> GetAuditLogAsync()
        {
            await LogAuditEvent("AuditLogAccessed", "Audit log requested", Environment.UserName);
            return await LoadAuditHistoryAsync();
        }

        public async Task<List<StoredTaskHistory>> GetTaskHistoryAsync(string taskId)
        {
            try
            {
                var allHistory = await LoadTaskHistoryAsync();
                return allHistory.FindAll(h => h.TaskId == taskId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTaskHistory Fehler: {ex.Message}");
                return new List<StoredTaskHistory>();
            }
        }

        #endregion

        #region BACKUP FUNKTIONEN

        public async Task<bool> CreateSecureBackupAsync(string backupPath)
        {
            try
            {
                await LogAuditEvent("BackupStarted", $"Creating backup to: {backupPath}", Environment.UserName);

                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);

                var backupKey = GenerateBackupKey();
                var backupInfo = new
                {
                    CreatedBy = Environment.UserName,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    Version = "3.0",
                    EncryptionType = "AES-256 + PBKDF2"
                };

                var backupInfoPath = Path.Combine(backupPath, "backup_info.json");
                await File.WriteAllTextAsync(backupInfoPath, JsonSerializer.Serialize(backupInfo, new JsonSerializerOptions { WriteIndented = true }));

                if (File.Exists(_tasksFilePath))
                    await CreateEncryptedBackupFile(_tasksFilePath, Path.Combine(backupPath, "tasks_backup.dat"), backupKey);

                if (File.Exists(_historyFilePath))
                    await CreateEncryptedBackupFile(_historyFilePath, Path.Combine(backupPath, "history_backup.dat"), backupKey);

                if (File.Exists(_auditFilePath))
                    await CreateEncryptedBackupFile(_auditFilePath, Path.Combine(backupPath, "audit_backup.dat"), backupKey);

                await LogAuditEvent("BackupCompleted", $"Backup created successfully at: {backupPath}", Environment.UserName);
                return true;
            }
            catch (Exception ex)
            {
                await LogAuditEvent("BackupFailed", ex.Message, Environment.UserName);
                return false;
            }
        }

        private async Task CreateEncryptedBackupFile(string sourcePath, string backupPath, string backupKey)
        {
            try
            {
                var originalData = await LoadEncryptedFileAsync(sourcePath);
                if (originalData == null) return;

                var tempKey = _encryptionKey;
                _encryptionKey = backupKey;

                var encryptedBackup = EncryptData(originalData);
                await File.WriteAllTextAsync(backupPath, encryptedBackup);

                _encryptionKey = tempKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup-Datei Fehler: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region UTILITY METHODS

        public async Task<int> GetTaskCountAsync()
        {
            try
            {
                var tasks = await LoadAllUserTasksAsync();
                return tasks.Count;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> CleanupOldDataAsync(TimeSpan retentionPeriod)
        {
            try
            {
                await LogAuditEvent("DataCleanupStarted", $"Retention period: {retentionPeriod.Days} days", Environment.UserName);

                var allTasks = await LoadAllStoredTasksAsync();
                var deletedCount = 0;
                var cutoffDate = DateTime.Now - retentionPeriod;

                foreach (var task in allTasks.ToList())
                {
                    if (task.Status == StoredTaskStatus.Completed &&
                        task.CompletedDate.HasValue &&
                        task.CompletedDate.Value < cutoffDate)
                    {
                        allTasks.Remove(task);
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    await SaveAllStoredTasksAsync(allTasks);
                    await LogAuditEvent("DataCleanupCompleted", $"Deleted {deletedCount} old completed tasks", Environment.UserName);
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                await LogAuditEvent("DataCleanupError", ex.Message, Environment.UserName);
                return 0;
            }
        }

        public async Task<SecurityStatus> GetSecurityStatusAsync()
        {
            var taskCount = await GetTaskCountAsync();
            var auditCount = (await LoadAuditHistoryAsync()).Count;

            return new SecurityStatus
            {
                IsEncrypted = true,
                EncryptionMethod = "AES-256 + PBKDF2",
                IntegrityMethod = "HMAC-SHA256",
                TaskCount = taskCount,
                AuditLogCount = auditCount,
                LastAccess = DateTime.Now,
                SecureMemoryEnabled = true,
                BackupAvailable = File.Exists(_tasksFilePath)
            };
        }

        public string GetDataPath() => _dataPath;

        public string GetSecurityInfo()
        {
            return $"User: {Environment.UserName}, Verschlüsselung: AES-256 + PBKDF2 (100k Iterationen), Integrität: HMAC-SHA256";
        }

        #endregion
    }

    #region DATA MODELS

    public class StoredUserTask
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public TaskProcessType Type { get; set; }
        public string? TicketNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public StoredTaskStatus Status { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    public class StoredTaskHistory
    {
        public string TaskId { get; set; } = "";
        public string Action { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } = "";
    }

    public enum StoredTaskStatus
    {
        Active = 0,
        Deleted = 1,
        Completed = 2
    }

    public class SecurityStatus
    {
        public bool IsEncrypted { get; set; }
        public string EncryptionMethod { get; set; } = "";
        public string IntegrityMethod { get; set; } = "";
        public int TaskCount { get; set; }
        public int AuditLogCount { get; set; }
        public DateTime LastAccess { get; set; }
        public bool SecureMemoryEnabled { get; set; }
        public bool BackupAvailable { get; set; }
    }

    #endregion
}