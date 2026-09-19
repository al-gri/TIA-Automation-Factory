using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal static class WhitelistManager
    {
        private const string ApplicationName = "TiaV21Worker.exe";
        private const string WhitelistBasePath = @"SOFTWARE\Siemens\Automation\Openness\Whitelist";

        public static void SynchronizeWhitelist()
        {
            string version = GetWhitelistVersion();
            string executablePath = GetExecutablePath();
            string fileHash = ComputeFileHash(executablePath);
            string dateModified = GetDateModified(executablePath);

            string entryKeyPath = $@"{WhitelistBasePath}\{version}\Entries\{ApplicationName}";

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey entryKey = baseKey.OpenSubKey(entryKeyPath, writable: true))
                {
                    if (entryKey == null)
                    {
                        throw new UnauthorizedAccessException(
                            $"Whitelist entry key not found or not writable: {entryKeyPath}. " +
                            "Run the elevated bootstrap script to grant permissions.");
                    }

                    entryKey.SetValue("Path", executablePath, RegistryValueKind.String);
                    entryKey.SetValue("DateModified", dateModified, RegistryValueKind.String);
                    entryKey.SetValue("FileHash", fileHash, RegistryValueKind.String);
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to synchronize whitelist: {ex.Message}", ex);
            }
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
            return lastWriteUtc.ToString("yyyy/MM/dd HH:mm:ss.fff");
        }

        public static bool IsBootstrapRequired()
        {
            string version = GetWhitelistVersion();
            string entryKeyPath = $@"{WhitelistBasePath}\{version}\Entries\{ApplicationName}";

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey entryKey = baseKey.OpenSubKey(entryKeyPath, writable: true))
                {
                    return entryKey == null;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}