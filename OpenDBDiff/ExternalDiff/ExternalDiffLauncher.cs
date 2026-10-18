using OpenDBDiff.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OpenDBDiff.ExternalDiff
{
    public class ExternalDiffLaunchException : Exception
    {
        public ExternalDiffLaunchException(string message) : base(message)
        {
        }

        public ExternalDiffLaunchException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public static class ExternalDiffLauncher
    {
        public static string RootTempPath
        {
            get { return Path.Combine(Path.GetTempPath(), nameof(OpenDBDiff), "extdiff"); }
        }

        public static void LaunchSelectedObject(ExternalDiffSettings settings, string oldSql, string newSql, string objectName, string oldTitle, string newTitle)
        {
            EnsureConfigured(settings);

            if (string.IsNullOrEmpty(oldSql) && string.IsNullOrEmpty(newSql))
                throw new ExternalDiffLaunchException("There is no SQL to compare for the selected object.");

            var sessionId = Guid.NewGuid().ToString("N");
            var sessionPath = Path.Combine(RootTempPath, sessionId);
            var oldDir = Path.Combine(sessionPath, "old");
            var newDir = Path.Combine(sessionPath, "new");
            Directory.CreateDirectory(oldDir);
            Directory.CreateDirectory(newDir);

            var fileName = ExternalDiffFileNames.SanitizeFileName(objectName);
            var oldFile = Path.Combine(oldDir, fileName);
            var newFile = Path.Combine(newDir, fileName);
            File.WriteAllText(oldFile, oldSql ?? string.Empty, Encoding.UTF8);
            File.WriteAllText(newFile, newSql ?? string.Empty, Encoding.UTF8);

            CleanupOldSessions(sessionId);
            Start(settings.ExecutablePath, ExternalDiffArguments.Apply(settings.FileArguments, oldFile, newFile, oldTitle, newTitle));
        }

        public static void LaunchAllDifferences(ExternalDiffSettings settings, IEnumerable<ExternalDiffPair> pairs, string oldTitle, string newTitle)
        {
            EnsureConfigured(settings);

            if (pairs == null)
                throw new ExternalDiffLaunchException("There are no schema differences to compare.");

            var sessionId = Guid.NewGuid().ToString("N");
            var sessionPath = Path.Combine(RootTempPath, sessionId);
            var oldDir = Path.Combine(sessionPath, "old");
            var newDir = Path.Combine(sessionPath, "new");
            Directory.CreateDirectory(oldDir);
            Directory.CreateDirectory(newDir);

            var wrote = false;
            foreach (var pair in pairs)
            {
                if (pair == null || string.IsNullOrWhiteSpace(pair.RelativePath))
                    continue;

                if (!string.IsNullOrEmpty(pair.OldSql))
                {
                    WriteRelative(oldDir, pair.RelativePath, pair.OldSql);
                    wrote = true;
                }

                if (!string.IsNullOrEmpty(pair.NewSql))
                {
                    WriteRelative(newDir, pair.RelativePath, pair.NewSql);
                    wrote = true;
                }
            }

            if (!wrote)
                throw new ExternalDiffLaunchException("There are no schema differences to compare.");

            CleanupOldSessions(sessionId);
            Start(settings.ExecutablePath, ExternalDiffArguments.Apply(settings.FolderArguments, oldDir, newDir, oldTitle, newTitle));
        }

        public static void TestExecutable(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                throw new ExternalDiffLaunchException("The external tool executable was not found.");

            Start(executablePath, string.Empty);
        }

        public static void CleanupOldSessions(string keepSessionId)
        {
            if (!Directory.Exists(RootTempPath))
                return;

            foreach (var directory in Directory.GetDirectories(RootTempPath))
            {
                if (string.Equals(Path.GetFileName(directory), keepSessionId, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        private static void WriteRelative(string root, string relativePath, string sql)
        {
            var fullPath = Path.Combine(root, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, sql ?? string.Empty, Encoding.UTF8);
        }

        private static void EnsureConfigured(ExternalDiffSettings settings)
        {
            if (settings == null || !settings.IsConfigured)
                throw new ExternalDiffLaunchException("Configure an external comparison tool before launching a diff.");
        }

        private static void Start(string executablePath, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments ?? string.Empty,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                throw new ExternalDiffLaunchException("Could not start the external comparison tool.", ex);
            }
        }
    }
}
