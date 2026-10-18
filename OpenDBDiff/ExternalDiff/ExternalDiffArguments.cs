using System;

namespace OpenDBDiff.ExternalDiff
{
    public static class ExternalDiffArguments
    {
        public static string Apply(string template, string left, string right, string leftTitle, string rightTitle)
        {
            if (string.IsNullOrWhiteSpace(template))
                template = "\"{left}\" \"{right}\"";

            return template
                .Replace("{lefttitle}", SanitizeTitle(leftTitle))
                .Replace("{righttitle}", SanitizeTitle(rightTitle))
                .Replace("{left}", left ?? string.Empty)
                .Replace("{right}", right ?? string.Empty);
        }

        public static string SanitizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;

            return title.Replace("\"", "'");
        }
    }
}
