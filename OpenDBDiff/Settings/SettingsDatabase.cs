using LiteDB;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace OpenDBDiff.Settings
{
    public sealed class SettingsDatabase : IDisposable
    {
        // Windows DPAPI blob version and provider GUID; this is not LiteDB password encryption.
        private static readonly byte[] WindowsProtectionHeader =
        {
            1, 0, 0, 0, 0xd0, 0x8c, 0x9d, 0xdf, 1, 0x15, 0xd1, 0x11,
            0x8c, 0x7a, 0, 0xc0, 0x4f, 0xc2, 0x97, 0xeb
        };

        private readonly string path;
        private readonly FileStream file;
        private readonly MemoryStream memory;
        private readonly byte[] initialData;
        private bool disposed;

        private SettingsDatabase(string path)
        {
            this.path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(this.path));
            // Allow atomic replacement and backup reads, but exclude concurrent writers.
            file = new FileStream(this.path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
            try
            {
                var header = new byte[WindowsProtectionHeader.Length];
                var read = file.Read(header, 0, header.Length);
                file.Position = 0;
                if (read == header.Length && header.SequenceEqual(WindowsProtectionHeader))
                {
                    using (var buffer = new MemoryStream())
                    {
                        file.CopyTo(buffer);
                        initialData = ProtectedData.Unprotect(buffer.ToArray(), null, DataProtectionScope.CurrentUser);
                    }
                    memory = new MemoryStream();
                    memory.Write(initialData, 0, initialData.Length);
                    memory.Position = 0;
                    Database = new LiteDatabase(memory);
                }
                else
                {
                    Database = new LiteDatabase(file);
                }
            }
            catch
            {
                memory?.Dispose();
                if (initialData != null)
                    Array.Clear(initialData, 0, initialData.Length);
                file.Dispose();
                throw;
            }
        }

        public LiteDatabase Database { get; private set; }

        public static SettingsDatabase Open()
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(OpenDBDiff));
            return Open(Path.Combine(directory, "settings.liteDb"));
        }

        public static SettingsDatabase Open(string path)
        {
            return new SettingsDatabase(path);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            byte[] updated = null;
            string temporaryPath = null;
            try
            {
                // Disposal checkpoints LiteDB's in-memory journal before we protect the data.
                Database.Dispose();
                if (memory == null)
                    return;

                updated = memory.ToArray();
                if (initialData.SequenceEqual(updated))
                    return;

                var encrypted = ProtectedData.Protect(updated, null, DataProtectionScope.CurrentUser);
                temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                {
                    output.Write(encrypted, 0, encrypted.Length);
                    output.Flush(true);
                }
                // Keep the previous encrypted version; never place decrypted settings on disk.
                File.Replace(temporaryPath, path, path + ".bak");
            }
            finally
            {
                memory?.Dispose();
                if (initialData != null)
                    Array.Clear(initialData, 0, initialData.Length);
                if (updated != null)
                    Array.Clear(updated, 0, updated.Length);
                file.Dispose();
                if (temporaryPath != null && File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}
