using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBDiff.Settings;
using System;
using System.IO;
using System.Security.Cryptography;

namespace OpenDBDiff.Tests.Settings
{
    [TestClass]
    public class SettingsDatabaseTests
    {
        private string directory;
        private string path;

        [TestInitialize]
        public void Initialize()
        {
            directory = Path.Combine(Path.GetTempPath(), "OpenDBDiff-settings-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "settings.liteDb");
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(directory, true);
        }

        [TestMethod]
        public void Open_WindowsProtectedSettings_ReadsSavedProjectWithoutChangingFile()
        {
            var original = CreateProtectedSettings();

            using (var settings = SettingsDatabase.Open(path))
                Assert.AreEqual("Saved project", settings.Database.GetCollection("projects").FindById(1)["name"].AsString);

            CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        }

        [TestMethod]
        public void Save_WindowsProtectedSettings_PreservesProjectsEncryptionAndBackup()
        {
            var original = CreateProtectedSettings();

            using (var settings = SettingsDatabase.Open(path))
                settings.Database.GetCollection("externalDiff").Insert(new BsonDocument { ["_id"] = 1, ["tool"] = "WinMerge" });

            var saved = File.ReadAllBytes(path);
            Assert.AreEqual((byte)1, saved[0]);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(path + ".bak"));
            var decoded = ProtectedData.Unprotect(saved, null, DataProtectionScope.CurrentUser);
            using (var stream = new MemoryStream(decoded))
            using (var db = new LiteDatabase(stream))
            {
                Assert.AreEqual("Saved project", db.GetCollection("projects").FindById(1)["name"].AsString);
                Assert.AreEqual("WinMerge", db.GetCollection("externalDiff").FindById(1)["tool"].AsString);
            }
        }

        [TestMethod]
        public void Open_NewDatabase_RemainsReadableByLiteDb()
        {
            using (var settings = SettingsDatabase.Open(path))
                settings.Database.GetCollection("projects").Insert(new BsonDocument { ["_id"] = 1 });

            using (var db = new LiteDatabase(path))
                Assert.IsNotNull(db.GetCollection("projects").FindById(1));
        }

        [TestMethod]
        public void Save_ExistingPlainDatabase_PreservesExistingData()
        {
            using (var db = new LiteDatabase(path))
                db.GetCollection("projects").Insert(new BsonDocument { ["_id"] = 1 });

            using (var settings = SettingsDatabase.Open(path))
                settings.Database.GetCollection("externalDiff").Insert(new BsonDocument { ["_id"] = 2 });

            using (var db = new LiteDatabase(path))
            {
                Assert.IsNotNull(db.GetCollection("projects").FindById(1));
                Assert.IsNotNull(db.GetCollection("externalDiff").FindById(2));
            }
        }

        [TestMethod]
        public void Save_WhenAtomicReplacementFails_PreservesOriginalAndRemovesTemporaryFile()
        {
            var original = CreateProtectedSettings();
            Directory.CreateDirectory(path + ".bak");
            try
            {
                using (var settings = SettingsDatabase.Open(path))
                    settings.Database.GetCollection("projects").Insert(new BsonDocument { ["_id"] = 2 });
                Assert.Fail("Replacement should fail when the backup path is a directory.");
            }
            catch (IOException)
            {
                CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
            }
            catch (UnauthorizedAccessException)
            {
                CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
            }
            Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
        }

        [TestMethod]
        public void Open_InvalidWindowsProtectedSettings_DoesNotOverwriteOriginal()
        {
            var original = CreateProtectedSettings();
            var truncated = new byte[24];
            Array.Copy(original, truncated, truncated.Length);
            File.WriteAllBytes(path, truncated);
            try
            {
                using (SettingsDatabase.Open(path)) { }
                Assert.Fail("Opening invalid protected settings should fail.");
            }
            catch (CryptographicException)
            {
                CollectionAssert.AreEqual(truncated, File.ReadAllBytes(path));
            }
            Assert.IsFalse(File.Exists(path + ".bak"));
        }

        private byte[] CreateProtectedSettings()
        {
            byte[] plain;
            using (var stream = new MemoryStream())
            {
                using (var db = new LiteDatabase(stream))
                    db.GetCollection("projects").Insert(new BsonDocument { ["_id"] = 1, ["name"] = "Saved project" });
                plain = stream.ToArray();
            }
            var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(path, encrypted);
            return encrypted;
        }
    }
}
