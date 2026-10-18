using OpenDBDiff.Abstractions.Schema;
using System.IO;
using System.Text;

namespace OpenDBDiff.ExternalDiff
{
    public static class ExternalDiffFileNames
    {
        public static string SanitizeFileName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "object.sql";

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(fullName.Length);

            foreach (var character in fullName)
            {
                if (character == '[' || character == ']')
                    continue;

                var isInvalid = false;
                foreach (var invalidCharacter in invalid)
                {
                    if (character == invalidCharacter)
                    {
                        isInvalid = true;
                        break;
                    }
                }

                builder.Append(isInvalid ? '_' : character);
            }

            var name = builder.ToString().Trim('.', ' ');
            if (name.Length == 0)
                name = "object";
            if (name.Length > 120)
                name = name.Substring(0, 120);

            return name + ".sql";
        }

        public static string RelativePath(ObjectType objectType, string fullName)
        {
            var folder = objectType == ObjectType.None ? "Other" : objectType.ToString();
            return Path.Combine(folder, SanitizeFileName(fullName));
        }
    }
}
