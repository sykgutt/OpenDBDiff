using System;
using System.IO;
using System.Linq;

namespace OpenDBDiff.Settings
{
    public class ExternalDiffSettings
    {
        public const string DefaultFileArguments = "\"{left}\" \"{right}\"";
        public const string DefaultFolderArguments = "\"{left}\" \"{right}\"";

        private const string CollectionName = "externalDiff";

        public Guid Id { get; set; }

        public string ExecutablePath { get; set; }

        public string FileArguments { get; set; }

        public string FolderArguments { get; set; }

        public bool IsConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ExecutablePath) && File.Exists(ExecutablePath);
            }
        }

        public static ExternalDiffSettings Get()
        {
            using (var settings = SettingsDatabase.Open())
            {
                var db = settings.Database;
                var stored = db.GetCollection<ExternalDiffSettings>(CollectionName).FindAll().FirstOrDefault();
                if (stored != null)
                {
                    if (string.IsNullOrWhiteSpace(stored.FileArguments))
                        stored.FileArguments = DefaultFileArguments;
                    if (string.IsNullOrWhiteSpace(stored.FolderArguments))
                        stored.FolderArguments = DefaultFolderArguments;
                    return stored;
                }
            }

            return new ExternalDiffSettings
            {
                Id = Guid.NewGuid(),
                FileArguments = DefaultFileArguments,
                FolderArguments = DefaultFolderArguments
            };
        }

        public static void Save(ExternalDiffSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (settings.Id == Guid.Empty)
                settings.Id = Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(settings.FileArguments))
                settings.FileArguments = DefaultFileArguments;
            if (string.IsNullOrWhiteSpace(settings.FolderArguments))
                settings.FolderArguments = DefaultFolderArguments;

            using (var database = SettingsDatabase.Open())
            {
                var db = database.Database;
                var collection = db.GetCollection<ExternalDiffSettings>(CollectionName);
                foreach (var extra in collection.FindAll().Where(item => item.Id != settings.Id).ToArray())
                    collection.Delete(extra.Id);

                collection.Upsert(settings);
            }
        }
    }
}
