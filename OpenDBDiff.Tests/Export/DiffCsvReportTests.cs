using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Export;
using OpenDBDiff.SqlServer.Schema.Model;
using OpenDBDiff.SqlServer.Schema.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenDBDiff.Tests.Export
{
    [TestClass]
    public class DiffCsvReportTests
    {
        private string directory;

        [TestInitialize]
        public void Initialize()
        {
            directory = Path.Combine(Path.GetTempPath(), "OpenDBDiff-csv-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(directory, true);
        }

        [TestMethod]
        public void Report_UsesCapturedNamesWhenDatabaseCloneLosesName()
        {
            var original = new Database { Name = "TargetReal", Options = new SqlOption(), Info = new DatabaseInfo() };
            var clone = (Database)original.Clone(null);
            Assert.IsTrue(string.IsNullOrEmpty(clone.Name));
            var report = new DiffCsvReport(new Database(), "SourceReal", original.Name);
            original.Name = "Changed";
            Assert.AreEqual("DiffTargetReal.csv", report.SuggestedFileName);
        }

        [TestMethod]
        public void Report_RejectsEmptyTargetName()
        {
            try { new DiffCsvReport(new Database(), "Source", " "); Assert.Fail("Expected an error"); }
            catch (ArgumentException ex) { StringAssert.Contains(ex.Message, "target"); }
        }

        [TestMethod]
        public void Report_MapsAllStatusesAndMatchesTreePrecedence()
        {
            var states = new[] { ObjectStatus.Original, ObjectStatus.Create, ObjectStatus.Drop, ObjectStatus.Alter,
                ObjectStatus.AlterWhitespace, ObjectStatus.AlterBody, ObjectStatus.Rebuild, ObjectStatus.Disabled,
                ObjectStatus.RebuildDependencies, ObjectStatus.Alter | ObjectStatus.AlterWhitespace,
                ObjectStatus.Rebuild | ObjectStatus.AlterWhitespace, ObjectStatus.ChangeOwner };
            var expected = new[] { "unchanged item", "new item", "missing item", "changed item data",
                "changed item view", "changed item data", "changed item data", "changed item data",
                "changed item data", "changed item view", "changed item data", "changed item data" };
            var db = new Database { Name = "Desired" };
            for (int i = 0; i < states.Length; i++)
                AddTable(db, "Case" + i, states[i]);

            var report = new DiffCsvReport(db, "Source", "Current");

            Assert.AreEqual(states.Length, report.Rows.Count);
            for (int i = 0; i < states.Length; i++)
                Assert.AreEqual(expected[i], report.Rows.Single(r => r.Name == "Case" + i).Difference);
            StringAssert.Contains(report.Rows.Single(r => r.Name == "Case1").Action, "Crear en BD target [Current]");
            StringAssert.Contains(report.Rows.Single(r => r.Name == "Case2").Action, "Eliminar de BD target [Current]");
            StringAssert.Contains(report.Rows.Single(r => r.Name == "Case3").Action, "Actualizar la definición");
            StringAssert.Contains(report.Rows.Single(r => r.Name == "Case4").Action, "Revisar diferencias de formato");
            StringAssert.Contains(report.Rows.Single(r => r.Name == "Case0").Action, "Sin acción");
            StringAssert.Contains(report.Rows.Single(r => r.Name == "Case10").Action, "reconstrucción");
        }

        [TestMethod]
        public void Report_IncludesTopLevelObjectsAndMemoryTablesWithoutChildrenOrSqlGeneration()
        {
            var db = new Database { Name = "Desired" };
            var table = AddTable(db, "Regular", ObjectStatus.Original);
            table.Columns.Add(new Column(table) { Name = "NestedColumn" });
            table.Indexes.Add(new Index(table) { Name = "NestedIndex" });
            var memory = AddTable(db, "Memory", ObjectStatus.Rebuild);
            memory.IsMemoryOptimized = true;
            db.Roles.Add(new Role(db) { Name = "RoleWithoutSchema" });

            var report = new DiffCsvReport(db, "Source", "Current");

            Assert.AreEqual(3, report.Rows.Count);
            Assert.AreEqual("Memory-Optimized Tables", report.Rows.Single(r => r.Name == "Memory").ObjectType);
            Assert.AreEqual("Tables", report.Rows.Single(r => r.Name == "Regular").ObjectType);
            var role = report.Rows.Single(r => r.Name == "RoleWithoutSchema");
            Assert.AreEqual("Roles", role.ObjectType);
            Assert.AreEqual("", role.Schema);
            Assert.AreEqual("[RoleWithoutSchema]", role.FullName);
        }

        [TestMethod]
        public void Report_DeduplicatesWithinTypeButKeepsSameFullNameForDifferentTypes()
        {
            var db = new Database();
            var table = AddTable(db, "Shared", ObjectStatus.Original);
            ((List<Table>)db.Tables).Add(table);
            ((List<View>)db.Views).Add(new View(db) { Name = "Shared", Owner = "dbo" });

            var report = new DiffCsvReport(db, "Source", "Current");

            Assert.AreEqual(2, report.Rows.Count);
            CollectionAssert.AreEqual(new[] { "Tables", "Views" }, report.Rows.Select(r => r.ObjectType).ToArray());
            Assert.IsTrue(report.Rows.All(r => r.FullName == "[dbo].[Shared]"));
        }

        [TestMethod]
        public void Report_UsesOrdinalOrderAndSnapshotsNamesStatesAndCurrentDatabase()
        {
            var db = new Database { Name = "Desired" };
            AddTable(db, "z", ObjectStatus.Original, "dbo");
            var first = AddTable(db, "A", ObjectStatus.Alter, "dbo");
            AddTable(db, "Z", ObjectStatus.Original, "A");
            var current = new Database { Name = "Actual" };
            var report = new DiffCsvReport(db, "Source", current.Name);
            first.Name = "ChangedAfterComparison";
            first.Status = ObjectStatus.Drop;
            current.Name = "DifferentSelection";

            Assert.AreEqual("DiffActual.csv", report.SuggestedFileName);
            CollectionAssert.AreEqual(new[] { "Z", "A", "z" }, report.Rows.Select(r => r.Name).ToArray());
            Assert.AreEqual("[dbo].[A]", report.Rows[1].FullName);
            Assert.AreEqual("changed item data", report.Rows[1].Difference);
        }

        [TestMethod]
        public void Report_SanitizesOnlyTheSuggestedFilename()
        {
            var db = new Database();
            AddTable(db, "Keep:This", ObjectStatus.Original);
            var report = new DiffCsvReport(db, "Source", "Actual:/?*\"<>|");

            Assert.AreEqual("DiffActual________.csv", report.SuggestedFileName);
            Assert.AreEqual("Keep:This", report.Rows[0].Name);
        }

        [TestMethod]
        public void Save_RoundTripsFiveColumnsAndUsesUtf8BomAndCrlf()
        {
            var db = new Database();
            AddTable(db, "Árbol,\"uno\"\r\ndos", ObjectStatus.AlterWhitespace, "esquema,ñ");
            var report = new DiffCsvReport(db, "Source", "Current");
            var path = Path.Combine(directory, "report.csv");

            report.Save(path);

            var bytes = File.ReadAllBytes(path);
            CollectionAssert.AreEqual(new byte[] { 0xef, 0xbb, 0xbf }, bytes.Take(3).ToArray());
            var text = File.ReadAllText(path, Encoding.UTF8);
            Assert.IsTrue(text.EndsWith("\r\n", StringComparison.Ordinal));
            var rows = ReadCsv(text);
            CollectionAssert.AreEqual(new[] { "Tipo de objeto", "Esquema", "Nombre del objeto", "Nombre completo", "Tipo de diferencia", "Acción" }, rows[0]);
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "Tables", "esquema,ñ", "Árbol,\"uno\"\r\ndos", "[esquema,ñ].[Árbol,\"uno\"\r\ndos]", "changed item view", "Revisar diferencias de formato entre BD source [Source] y BD target [Current]; contenido equivalente según las opciones de comparación." }, rows[1]);
            Assert.IsTrue(rows.All(r => r.Length == 6));
        }

        [TestMethod]
        public void Save_EscapesRealDatabaseNamesInActionWithoutSanitizingThem()
        {
            var db = new Database();
            AddTable(db, "Table", ObjectStatus.Create);
            var report = new DiffCsvReport(db, "Origen,\"México\"", "Destino:/?ñ");
            var path = Path.Combine(directory, "names.csv");
            report.Save(path);
            var rows = ReadCsv(File.ReadAllText(path));
            Assert.AreEqual(6, rows[1].Length);
            StringAssert.Contains(rows[1][5], "[Origen,\"México\"]");
            StringAssert.Contains(rows[1][5], "[Destino:/?ñ]");
            Assert.AreEqual("DiffDestino___ñ.csv", report.SuggestedFileName);
        }

        [TestMethod]
        public void Save_EmptyComparison_WritesHeaderOnly()
        {
            var report = new DiffCsvReport(new Database(), "Source", "Current");
            var path = Path.Combine(directory, "empty.csv");
            report.Save(path);

            Assert.AreEqual(1, ReadCsv(File.ReadAllText(path)).Count);
        }

        [TestMethod]
        public void Save_ReplacesExistingFileWithoutLeavingTemporaryFiles()
        {
            var path = Path.Combine(directory, "report.csv");
            File.WriteAllText(path, "Existing file");
            new DiffCsvReport(new Database(), "Source", "Current").Save(path);

            StringAssert.StartsWith(File.ReadAllText(path), "Tipo de objeto,");
            Assert.AreEqual(1, Directory.GetFiles(directory).Length);
        }

        [TestMethod]
        public void Save_LockedDestination_PreservesOriginalAndCleansTemporaryFile()
        {
            var path = Path.Combine(directory, "report.csv");
            File.WriteAllText(path, "Original bytes");
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                try
                {
                    new DiffCsvReport(new Database(), "Source", "Current").Save(path);
                    Assert.Fail("Writing a locked destination should fail.");
                }
                catch (IOException) { }
            }
            Assert.AreEqual("Original bytes", File.ReadAllText(path));
            Assert.AreEqual(1, Directory.GetFiles(directory).Length);
        }

        private static Table AddTable(Database db, string name, ObjectStatus status, string schema = "dbo")
        {
            db.Options = db.Options ?? new SqlOption();
            db.Info = db.Info ?? new DatabaseInfo();
            var table = new Table(db) { Name = name, Owner = schema, Status = status };
            db.Tables.Add(table);
            return table;
        }

        private static List<string[]> ReadCsv(string text)
        {
            var result = new List<string[]>();
            using (var parser = new TextFieldParser(new StringReader(text)))
            {
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;
                while (!parser.EndOfData)
                    result.Add(parser.ReadFields());
            }
            return result;
        }
    }
}
