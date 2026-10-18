using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBDiff.SqlServer.Schema.Model;
using OpenDBDiff.SqlServer.Schema.Options;
using OpenDBDiff.SqlServer.Schema.Generates;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Model;
using System.Collections.Generic;
using System.Linq;
using SqlIndex = OpenDBDiff.SqlServer.Schema.Model.Index;
namespace OpenDBDiff.Tests.Model.Tests
{
    [TestClass]
    public class MemoryOptimizedTests
    {
        [TestMethod]
        public void TableMustPreserveMemoryOptimizedMetadata()
        {
            Assert.IsNotNull(typeof(Table).GetProperty("IsMemoryOptimized"));
            Assert.IsNotNull(typeof(Table).GetProperty("Durability"));
        }
        [TestMethod]
        public void HashIndexMustPreserveBucketCount()
        {
            Assert.IsNotNull(typeof(Index).GetProperty("BucketCount"));
        }

        private static Database Database(float version = 17)
        {
            return new Database
            {
                Name = "TestMemory",
                Options = new SqlOption(),
                Info = new DatabaseInfo { VersionNumber = version, IsXtpSupported = true,
                    HasMemoryOptimizedFileGroup = true, Collation = "Latin1_General_100_BIN2" }
            };
        }

        private static Table MemoryTable(Database database, string name = "MemoryData", int id = 10)
        {
            var table = new Table(database) { Id = id, Name = name, Owner = "dbo",
                IsMemoryOptimized = true, Durability = "SCHEMA_ONLY" };
            table.Columns.Add(new Column(table) { Id = 1, Name = "Id", Type = "int", IsNullable = false });
            var index = new SqlIndex(table) { Id = 2, Name = "IX_Id", Type = SqlIndex.IndexTypeEnum.Hash, BucketCount = 128 };
            index.Columns.Add(new IndexColumn(table) { Id = 1, Name = "Id", KeyOrder = 1 });
            table.Indexes.Add(index);
            database.Tables.Add(table);
            return table;
        }

        [TestMethod]
        public void MemoryDefinitionContainsHashAndDurabilityWithoutDiskOptions()
        {
            var table = MemoryTable(Database());
            var sql = table.ToSql();
            StringAssert.Contains(sql, "MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_ONLY");
            StringAssert.Contains(sql, "NONCLUSTERED HASH ([Id]) WITH (BUCKET_COUNT = 128)");
            Assert.IsFalse(sql.Contains("PAD_INDEX"));
            Assert.IsFalse(sql.Contains("CREATE NONCLUSTERED"));
        }

        [TestMethod]
        public void ClonePreservesMemoryAndHashMetadata()
        {
            var database = Database();
            var table = MemoryTable(database);
            table.SchemaBoundDependents.Add("[dbo].[NativeProcedure]");
            var copy = (Table)table.Clone(database);
            Assert.IsTrue(copy.IsMemoryOptimized);
            Assert.AreEqual("SCHEMA_ONLY", copy.Durability);
            Assert.AreEqual(128L, copy.Indexes[0].BucketCount);
            Assert.AreEqual(table.ToSql(), copy.ToSql());
            copy.SchemaBoundDependents.Clear();
            Assert.AreEqual(1, table.SchemaBoundDependents.Count);
        }

        [TestMethod]
        public void EqualMemorySchemasHaveNoDifferences()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); MemoryTable(desired);
            var comparison = Generate.Compare(old, desired);
            Assert.AreEqual(ObjectStatus.Original, comparison.Tables[0].Status);
            Assert.IsFalse(comparison.ToSqlDiff(new List<ISchemaBase>()).ToSQL().Contains("ALTER TABLE"));
        }

        [TestMethod]
        public void MemoryIndexIgnoresDiskOnlyStatisticsOption()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old).Indexes[0].IsAutoStatistics = false;
            MemoryTable(desired).Indexes[0].IsAutoStatistics = true;

            var comparison = Generate.Compare(old, desired);

            Assert.AreEqual(ObjectStatus.Original, comparison.Tables[0].Status);
        }

        [TestMethod]
        public void ChangedHashIsDetectedAndUsesAlterTable()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); MemoryTable(desired).Indexes[0].BucketCount = 256;
            var table = Generate.Compare(old, desired).Tables[0];
            Assert.AreEqual(ObjectStatus.Alter, table.Status);
            Assert.AreEqual(128L, table.OriginalTable.Indexes[0].BucketCount);
            var sql = old.ToSqlDiff(new List<ISchemaBase>()).ToSQL();
            StringAssert.Contains(sql, "ALTER TABLE [dbo].[MemoryData] ALTER INDEX [IX_Id] REBUILD");
            StringAssert.Contains(sql, "BUCKET_COUNT = 256");
            Assert.IsFalse(sql.Contains("CREATE INDEX"));
        }

        [TestMethod]
        public void AddNullableColumnUsesDirectAlter()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); var table = MemoryTable(desired);
            table.Columns.Add(new Column(table) { Id = 3, Name = "Added", Type = "int", IsNullable = true });
            Generate.Compare(old, desired);
            StringAssert.Contains(old.ToSqlDiff(new List<ISchemaBase>()).ToSQL(), "ADD [Added] [int] NULL");
        }

        [TestMethod]
        public void NewMemoryTableGeneratesCreate()
        {
            var old = Database(); var desired = Database(); MemoryTable(desired);
            Generate.Compare(old, desired);
            StringAssert.Contains(old.ToSqlDiff(new List<ISchemaBase>()).ToSQL(), "CREATE TABLE [dbo].[MemoryData]");
        }

        [TestMethod]
        public void RemovedMemoryTableGeneratesDrop()
        {
            var old = Database(); MemoryTable(old);
            Generate.Compare(old, Database());
            StringAssert.Contains(old.ToSqlDiff(new List<ISchemaBase>()).ToSQL(), "DROP TABLE [dbo].[MemoryData]");
        }

        private static string Blocked(Database database, ICollection<ISchemaBase> selection = null)
        {
            try { database.ToSqlDiff(selection ?? new List<ISchemaBase>()); }
            catch (ScriptGenerationException error) { return error.Message; }
            Assert.Fail("Script must be blocked");
            return null;
        }

        [TestMethod]
        public void MemoryConversionAndDurabilityChangeAreExplained()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old).IsMemoryOptimized = false;
            MemoryTable(desired).Durability = "SCHEMA_AND_DATA";
            Generate.Compare(old, desired);
            var message = Blocked(old);
            StringAssert.Contains(message, "conversion between disk and memory");
            StringAssert.Contains(message, "changing DURABILITY");
        }

        [TestMethod]
        public void Sql2014CanCreateButCannotAlterMemoryTables()
        {
            var old = Database(12); var desired = Database(12);
            MemoryTable(old); MemoryTable(desired).Indexes[0].BucketCount = 256;
            Generate.Compare(old, desired);
            StringAssert.Contains(Blocked(old), "SQL Server 2016+");
            var empty = Database(12);
            var create = Database(12); MemoryTable(create);
            Generate.Compare(empty, create);
            StringAssert.Contains(empty.ToSqlDiff(new List<ISchemaBase>()).ToSQL(), "CREATE TABLE");
        }

        [TestMethod]
        public void MissingDestinationRequirementsAreAggregated()
        {
            var old = Database(11); old.Info.IsXtpSupported = false; old.Info.HasMemoryOptimizedFileGroup = false;
            var desired = Database(); MemoryTable(desired, "One"); MemoryTable(desired, "Two", 20);
            Generate.Compare(old, desired);
            var message = Blocked(old);
            StringAssert.Contains(message, "[dbo].[One]");
            StringAssert.Contains(message, "[dbo].[Two]");
            StringAssert.Contains(message, "filegroup");
            StringAssert.Contains(message, "2014+");
        }

        [TestMethod]
        public void SchemaBoundDependenciesBlockAutomaticChanges()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old).SchemaBoundDependents.Add("[dbo].[NativeProcedure]");
            MemoryTable(desired).Indexes[0].BucketCount = 256;
            Generate.Compare(old, desired);
            StringAssert.Contains(Blocked(old), "[dbo].[NativeProcedure]");
        }

        [TestMethod]
        public void SchemaBoundDependencyDoesNotBlockDefaultOnlyChange()
        {
            var old = Database(); var desired = Database();
            var oldTable = MemoryTable(old); var desiredTable = MemoryTable(desired);
            var oldTimestamp = new Column(oldTable) { Id = 2, Name = "Timestamp", Type = "datetime", IsNullable = true };
            oldTimestamp.DefaultConstraint = new ColumnConstraint(oldTimestamp)
            {
                Name = "DF_MemoryData_Timestamp",
                Type = Constraint.ConstraintType.Default,
                Definition = "(getdate())"
            };
            oldTable.Columns.Add(oldTimestamp);
            desiredTable.Columns.Add(new Column(desiredTable) { Id = 2, Name = "Timestamp", Type = "datetime", IsNullable = true });
            oldTable.SchemaBoundDependents.Add("[dbo].[NativeProcedure]");

            Generate.Compare(old, desired);

            string sql = old.ToSqlDiff(new List<ISchemaBase>()).ToSQL();
            StringAssert.Contains(sql, "DROP CONSTRAINT [DF_MemoryData_Timestamp]");
        }

        [TestMethod]
        public void SelectingDiskTableExcludesBlockedMemoryTable()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); MemoryTable(desired).Durability = "SCHEMA_AND_DATA";
            var disk = new Table(old) { Name = "Disk", Owner = "dbo", Id = 20 };
            old.Tables.Add(disk);
            Generate.Compare(old, desired);
            var sql = old.ToSqlDiff(new List<ISchemaBase> { disk }).ToSQL();
            Assert.IsFalse(sql.Contains("MemoryData"));
            StringAssert.Contains(sql, "DROP TABLE [dbo].[Disk]");
        }

        [TestMethod]
        public void VersionCapabilitiesUseRealMajorVersion()
        {
            Assert.AreEqual(17, Database(17).Info.MajorVersion);
            Assert.IsTrue(Database(17).Info.SupportsMemoryOptimizedAlter);
            Assert.IsFalse(Database(12).Info.SupportsMemoryOptimizedAlter);
            Assert.IsFalse(Database(11).Info.SupportsMemoryOptimizedTables);
        }

        [TestMethod]
        public void SelectingTableAndOneChildStillIncludesAllTableChanges()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); var desiredTable = MemoryTable(desired);
            desiredTable.Columns.Add(new Column(desiredTable) { Id = 5, Name = "Added", Type = "int", IsNullable = true });
            desiredTable.Indexes[0].BucketCount = 256;
            Generate.Compare(old, desired);
            var table = old.Tables[0];
            var sql = old.ToSqlDiff(new List<ISchemaBase> { table, table.Indexes[0] }).ToSQL();
            StringAssert.Contains(sql, "ADD [Added]");
            StringAssert.Contains(sql, "BUCKET_COUNT = 256");
        }

        [TestMethod]
        public void SelectingUnchangedChildDoesNotApplyOtherChanges()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); MemoryTable(desired).Indexes[0].BucketCount = 256;
            Generate.Compare(old, desired);
            var sql = old.ToSqlDiff(new List<ISchemaBase> { old.Tables[0].Columns[0] }).ToSQL();
            Assert.IsFalse(sql.Contains("ALTER TABLE"));
        }

        [TestMethod]
        public void SameIndexIdOnDifferentTablesDoesNotLeakIntoSelection()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old, "One", 10); MemoryTable(old, "Two", 20);
            MemoryTable(desired, "One", 10).Indexes[0].BucketCount = 256;
            MemoryTable(desired, "Two", 20).Indexes[0].BucketCount = 512;
            Generate.Compare(old, desired);
            var sql = old.ToSqlDiff(new List<ISchemaBase> { old.Tables[0].Indexes[0] }).ToSQL();
            StringAssert.Contains(sql, "BUCKET_COUNT = 256");
            Assert.IsFalse(sql.Contains("512"));
        }

        [TestMethod]
        public void NewTableIsNotBlockedByDependenciesThatOnlyExistInDesiredDatabase()
        {
            var old = Database(); var desired = Database();
            MemoryTable(desired).SchemaBoundDependents.Add("[dbo].[DesiredOnlyNativeProc]");
            Generate.Compare(old, desired);
            StringAssert.Contains(old.ToSqlDiff(new List<ISchemaBase>()).ToSQL(), "CREATE TABLE");
        }

        [TestMethod]
        public void InvalidHashAndTableRebuildAreBlocked()
        {
            var database = Database(); var table = MemoryTable(database);
            table.Indexes[0].BucketCount = 0;
            table.Status = ObjectStatus.Rebuild;
            var message = Blocked(database);
            StringAssert.Contains(message, "invalid HASH");
            StringAssert.Contains(message, "reconstruction");
        }

        [TestMethod]
        public void TableReaderFailsInsteadOfReturningPartialCatalog()
        {
            var database = Database();
            var logs = new List<OpenDBDiff.Abstractions.Schema.Errors.MessageLog>();
            try { new GenerateTables(new Generate()).Fill(database, "unsupported_keyword=value", logs); }
            catch (OpenDBDiff.Abstractions.Schema.Misc.SchemaException error)
            {
                StringAssert.Contains(error.Message, "Reading tables/columns/constraints in TestMemory");
                Assert.AreEqual(0, logs.Count);
                return;
            }
            Assert.Fail("A failed read must abort the comparison, rather than return a partial catalog");
        }

        [TestMethod]
        public void IncompleteMetadataCannotBeScriptedAsACompleteMemoryTable()
        {
            var database = Database(); var table = MemoryTable(database);
            table.Status = ObjectStatus.Create;
            database.Options.Ignore.FilterConstraintPK = false;
            StringAssert.Contains(Blocked(database), "requires complete");
        }

        [TestMethod]
        public void ColumnReorderingIsDetectedAndBlockedAsReconstruction()
        {
            var old = Database(); var desired = Database();
            var oldTable = MemoryTable(old); var desiredTable = MemoryTable(desired);
            oldTable.Columns.Add(new Column(oldTable) { Id = 2, Name = "Other", Type = "int", IsNullable = true });
            desiredTable.Columns.Add(new Column(desiredTable) { Id = 2, Name = "Other", Type = "int", IsNullable = true });
            desiredTable.Columns.Reverse();
            Generate.Compare(old, desired);
            Assert.AreEqual(ObjectStatus.Alter, old.Tables[0].Status);
            StringAssert.Contains(Blocked(old), "column order requires table reconstruction");
        }

        [TestMethod]
        public void UnhandledIndexTypeIsDetectedRatherThanRenderedAsNonclustered()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); MemoryTable(desired).Indexes[0].Type = (SqlIndex.IndexTypeEnum)5;
            Generate.Compare(old, desired);
            Assert.AreEqual(ObjectStatus.Alter, old.Tables[0].Status);
            StringAssert.Contains(Blocked(old), "unsupported memory index type");
            StringAssert.Contains(old.Tables[0].ToSql(), "Unsupported memory index type 5");
        }

        [TestMethod]
        public void CaseInsensitiveIdentifiersDoNotCauseColumnDropAndAdd()
        {
            var old = Database(); var desired = Database();
            MemoryTable(old); var table = MemoryTable(desired);
            table.Columns[0].Name = "id";
            table.Indexes[0].Columns[0].Name = "id";
            Generate.Compare(old, desired);
            Assert.AreEqual(ObjectStatus.Original, old.Tables[0].Status);
            Assert.IsFalse(old.ToSqlDiff(new List<ISchemaBase>()).ToSQL().Contains("DROP COLUMN"));
        }

        [TestMethod]
        public void DefaultLiteralCaseIsPreservedEvenWithCaseInsensitiveIdentifiers()
        {
            var old = Database(); var desired = Database();
            var oldTable = MemoryTable(old); var desiredTable = MemoryTable(desired);
            var first = new Column(oldTable) { Id = 2, Name = "Code", Type = "varchar", Size = 10, IsNullable = true };
            first.DefaultConstraint = new ColumnConstraint(first) { Name = "DF_Code", Type = Constraint.ConstraintType.Default, Definition = "('UPPER')" };
            oldTable.Columns.Add(first);
            var second = first.Clone(desiredTable);
            second.DefaultConstraint.Definition = "('upper')";
            desiredTable.Columns.Add(second);
            Generate.Compare(old, desired);
            Assert.AreEqual(ObjectStatus.Alter, old.Tables[0].Status);
            StringAssert.Contains(old.ToSqlDiff(new List<ISchemaBase>()).ToSQL(), "DEFAULT ('upper')");
        }
    }
}
