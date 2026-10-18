using System;
using System.IO;

namespace OpenDBDiff.ExternalDiff
{
    public class ExternalDiffPreset
    {
        public string Name { get; set; }

        public string FileArguments { get; set; }

        public string FolderArguments { get; set; }

        public string[] RelativeExecutables { get; set; }
    }

    public static class ExternalDiffPresets
    {
        public static readonly ExternalDiffPreset WinMerge = new ExternalDiffPreset
        {
            Name = "WinMerge",
            FileArguments = "/e /u /dl \"{lefttitle}\" /dr \"{righttitle}\" \"{left}\" \"{right}\"",
            FolderArguments = "/r /e /u /dl \"{lefttitle}\" /dr \"{righttitle}\" \"{left}\" \"{right}\"",
            RelativeExecutables = new[]
            {
                Path.Combine("WinMerge", "WinMergeU.exe"),
                Path.Combine("WinMerge", "WinMerge.exe"),
                "WinMergeU.exe"
            }
        };

        public static readonly ExternalDiffPreset BeyondCompare = new ExternalDiffPreset
        {
            Name = "Beyond Compare",
            FileArguments = "\"{left}\" \"{right}\"",
            FolderArguments = "\"{left}\" \"{right}\"",
            RelativeExecutables = new[]
            {
                Path.Combine("Beyond Compare 5", "BCompare.exe"),
                Path.Combine("Beyond Compare 4", "BCompare.exe"),
                Path.Combine("Beyond Compare 3", "BCompare.exe"),
                "BCompare.exe"
            }
        };

        public static string FindExecutable(ExternalDiffPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            foreach (var root in GetSearchRoots())
            {
                foreach (var relative in preset.RelativeExecutables)
                {
                    var candidate = Path.Combine(root, relative);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            return null;
        }

        public static string[] GetSearchRoots()
        {
            return new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };
        }
    }
}
