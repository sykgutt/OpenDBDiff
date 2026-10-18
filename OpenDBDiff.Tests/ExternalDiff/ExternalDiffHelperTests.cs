using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.ExternalDiff;
using System.IO;

namespace OpenDBDiff.Tests.ExternalDiff
{
    [TestClass]
    public class ExternalDiffHelperTests
    {
        [TestMethod]
        public void SanitizeFileName_StripsBrackets()
        {
            var name = ExternalDiffFileNames.SanitizeFileName("[dbo].[Customers]");
            Assert.AreEqual("dbo.Customers.sql", name);
        }

        [TestMethod]
        public void SanitizeFileName_ReplacesInvalidCharacters()
        {
            var name = ExternalDiffFileNames.SanitizeFileName("dbo.A/B:C*D?");
            Assert.IsFalse(name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0);
            Assert.IsTrue(name.EndsWith(".sql"));
        }

        [TestMethod]
        public void SanitizeFileName_UsesFallbackForEmpty()
        {
            Assert.AreEqual("object.sql", ExternalDiffFileNames.SanitizeFileName("   "));
        }

        [TestMethod]
        public void RelativePath_IncludesObjectTypeFolder()
        {
            var relative = ExternalDiffFileNames.RelativePath(ObjectType.StoredProcedure, "[dbo].[usp_Get]");
            Assert.AreEqual(Path.Combine("StoredProcedure", "dbo.usp_Get.sql"), relative);
        }

        [TestMethod]
        public void Apply_ReplacesPlaceholders()
        {
            var args = ExternalDiffArguments.Apply(
                "/e \"{left}\" \"{right}\" /dl \"{lefttitle}\" /dr \"{righttitle}\"",
                @"C:\old.sql",
                @"C:\new.sql",
                "Origin DB",
                "Destination DB");

            Assert.AreEqual("/e \"C:\\old.sql\" \"C:\\new.sql\" /dl \"Origin DB\" /dr \"Destination DB\"", args);
        }

        [TestMethod]
        public void Apply_ReplacesLeftTitleBeforeLeft()
        {
            var args = ExternalDiffArguments.Apply("{lefttitle}-{left}", "FILE", "OTHER", "TITLE", "R");
            Assert.AreEqual("TITLE-FILE", args);
            Assert.IsFalse(args.Contains("{left"));
        }

        [TestMethod]
        public void Apply_UsesDefaultTemplateWhenEmpty()
        {
            var args = ExternalDiffArguments.Apply(null, "a.sql", "b.sql", "L", "R");
            Assert.AreEqual("\"a.sql\" \"b.sql\"", args);
        }

        [TestMethod]
        public void SanitizeTitle_ReplacesQuotes()
        {
            Assert.AreEqual("Prod 'main'", ExternalDiffArguments.SanitizeTitle("Prod \"main\""));
        }

        [TestMethod]
        public void WinMergePreset_ContainsFileAndFolderPlaceholders()
        {
            Assert.IsTrue(ExternalDiffPresets.WinMerge.FileArguments.Contains("{left}"));
            Assert.IsTrue(ExternalDiffPresets.WinMerge.FileArguments.Contains("{right}"));
            Assert.IsTrue(ExternalDiffPresets.WinMerge.FolderArguments.Contains("/r"));
            Assert.IsTrue(ExternalDiffPresets.WinMerge.FolderArguments.Contains("{left}"));
        }

        [TestMethod]
        public void BeyondComparePreset_UsesQuotedPaths()
        {
            var args = ExternalDiffArguments.Apply(
                ExternalDiffPresets.BeyondCompare.FileArguments,
                @"C:\left.sql",
                @"C:\right.sql",
                "L",
                "R");
            Assert.AreEqual("\"C:\\left.sql\" \"C:\\right.sql\"", args);
        }
    }
}
