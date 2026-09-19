using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal static class WhitelistManager
    {
        private const string ApplicationName = "TiaV21Worker.exe";
        private const string WhitelistBasePath = @"SOFTWARE\Siemens\Automation\Openness\Whitelist";

        public static WhitelistSyncResult SynchronizeWhitelist()
        {
            string version = GetWhitelistVersion();
            string executablePath = GetExecutablePath();
            string fileHash = ComputeFileHash(executablePath);
            string dateModified = GetDateModified(executablePath);

            string entryKeyPath = $@"{WhitelistBasePath}\{version}\Entries\{ApplicationName}\Entry";

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey entryKey = baseKey.OpenSubKey(entryKeyPath, writable: true))
                {
                    if (entryKey == null)
                    {
                        return WhitelistSyncResult.BootstrapRequired();
                    }

                    entryKey.SetValue("Path", executablePath, RegistryValueKind.String);
                    entryKey.SetValue("DateModified", dateModified, RegistryValueKind.String);
                    entryKey.SetValue("FileHash", fileHash, RegistryValueKind.String);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return WhitelistSyncResult.BootstrapRequired();
            }
            catch (Exception)
            {
                return WhitelistSyncResult.BootstrapRequired();
            }

            return WhitelistSyncResult.Success();
        }

        private static string GetWhitelistVersion()
        {
            Assembly engineeringAssembly = typeof(Siemens.Engineering.TiaPortal).Assembly;
            Version version = engineeringAssembly.GetName().Version;
            return $"{version.Major}.{version.Minor}";
        }

        private static string GetExecutablePath()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            return Path.GetFullPath(location);
        }

        private static string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return Convert.ToBase64String(hash);
            }
        }

        private static string GetDateModified(string filePath)
        {
            DateTime lastWriteUtc = File.GetLastWriteTimeUtc(filePath);
            return lastWriteUtc.ToString("yyyy/MM/dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }
    }

    internal sealed class WhitelistSyncResult
    {
        public bool Success { get; private set; }
        public bool BootstrapRequired { get; private set; }
        public string Message { get; private set; }

        private WhitelistSyncResult(bool success, bool bootstrapRequired, string message)
        {
            Success = success;
            BootstrapRequired = bootstrapRequired;
            Message = message;
        }

        public static WhitelistSyncResult Success()
        {
            return new WhitelistSyncResult(true, false, null);
        }

        public static WhitelistSyncResult BootstrapRequired()
        {
            return new WhitelistSyncResult(false, true, "Whitelist synchronization requires elevated bootstrap. Run the bootstrap script to grant registry permissions.");
        }
    }
}