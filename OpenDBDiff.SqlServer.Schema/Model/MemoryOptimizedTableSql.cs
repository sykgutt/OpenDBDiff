using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Model;
using OpenDBDiff.Abstractions.Schema.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenDBDiff.SqlServer.Schema.Model
{
    public sealed class ScriptGenerationException : SchemaException
    {
        public ScriptGenerationException(IEnumerable<string> reasons)
            : base("Script generation blocked:\r\n" + string.Join("\r\n", reasons)) { }
    }

    // One implementation shared by GUI, CLI, object exports and direct updates.
    public static class MemoryOptimizedTableSql
    {
        public static string Quote(string name) { return "[" + name.Replace("]", "]]") + "]"; }

        public static bool IsMemoryTable(Table table)
        {
            return table.IsMemoryOptimized || (table.OriginalTable != null && table.OriginalTable.IsMemoryOptimized);
        }

        private static StringComparer NameComparer(ISchemaBase item)
        {
            return item.RootParent.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        }

        private static bool SameIdentity(ISchemaBase first, ISchemaBase second)
        {
            return first.ObjectType == second.ObjectType && NameComparer(second).Equals(first.FullName, second.FullName);
        }

        public static bool Selected(ISchemaBase item, ICollection<ISchemaBase> selection)
        {
            return selection == null || selection.Count == 0 || selection.Any(s =>
                SameIdentity(s, item));
        }

        public static bool IncludesTable(Table table, ICollection<ISchemaBase> selection)
        {
            return Selected(table, selection) || selection.Any(s => OwningTable(s) == table ||
                (OwningTable(s) != null && SameIdentity(OwningTable(s), table)));
        }

        public static Table OwningTable(ISchemaBase item)
        {
            while (item != null && !(item is Table)) item = item.Parent;
            return item as Table;
        }

        public static string IndexDefinition(Index index, IEnumerable<string> columns, string key = null)
        {
            if (index.Type != Index.IndexTypeEnum.Hash && index.Type != Index.IndexTypeEnum.Nonclustered)
                return "/* Unsupported memory index type " + (int)index.Type + ": " + Quote(index.Name) + " */";
            string head = key == null ? "INDEX " + Quote(index.Name) : "CONSTRAINT " + Quote(index.Name) + " " + key;
            if (key == null && index.IsUniqueKey) head += " UNIQUE";
            head += " NONCLUSTERED" + (index.Type == Index.IndexTypeEnum.Hash ? " HASH" : "");
            head += " (" + string.Join(", ", columns) + ")";
            if (index.Type == Index.IndexTypeEnum.Hash) head += " WITH (BUCKET_COUNT = " + index.BucketCount + ")";
            return head;
        }

        public static string IndexDefinition(Index index)
        {
            return IndexDefinition(index, index.Columns.OrderBy(c => c.KeyOrder).Select(c =>
                Quote(c.Name) + (index.Type == Index.IndexTypeEnum.Hash ? "" : (c.Order ? " DESC" : " ASC"))));
        }

        public static string ConstraintDefinition(Constraint constraint)
        {
            if (constraint.Type == Constraint.ConstraintType.PrimaryKey || constraint.Type == Constraint.ConstraintType.Unique)
                return IndexDefinition(constraint.Index, constraint.Columns.OrderBy(c => c.KeyOrder).Select(c =>
                    Quote(c.Name) + (constraint.Index.Type == Index.IndexTypeEnum.Hash ? "" : (c.Order ? " DESC" : " ASC"))),
                    constraint.Type == Constraint.ConstraintType.PrimaryKey ? "PRIMARY KEY" : "UNIQUE");
            if (constraint.Type == Constraint.ConstraintType.Check)
                return "CONSTRAINT " + Quote(constraint.Name) + " CHECK " + constraint.Definition;
            return constraint.ToSql();
        }

        public static string Create(Table table)
        {
            var definitions = table.Columns.Where(c => !c.HasState(ObjectStatus.Drop)).Select(c => c.ToSql()).ToList();
            definitions.AddRange(table.Constraints.Where(c => !c.HasState(ObjectStatus.Drop)).Select(ConstraintDefinition));
            definitions.AddRange(table.Indexes.Where(i => !i.HasState(ObjectStatus.Drop)).Select(IndexDefinition));
            return "CREATE TABLE " + table.FullName + "\r\n(\r\n    " + string.Join(",\r\n    ", definitions) +
                "\r\n) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = " + table.Durability + ");\r\nGO\r\n";
        }

        private static bool SameDefault(Column first, Column second)
        {
            return first.DefaultConstraint == null && second.DefaultConstraint == null ||
                first.DefaultConstraint != null && second.DefaultConstraint != null &&
                NameComparer(second).Equals(first.DefaultConstraint.Name, second.DefaultConstraint.Name) &&
                ColumnConstraint.Compare(first.DefaultConstraint, second.DefaultConstraint);
        }

        private static bool SameMemoryIndex(Index first, Index second)
        {
            return first.Type == second.Type && first.BucketCount == second.BucketCount &&
                first.IsUniqueKey == second.IsUniqueKey && IndexColumns.Compare(first.Columns, second.Columns);
        }

        private static bool SameConstraint(Constraint first, Constraint second)
        {
            bool firstIsKey = first.Type == Constraint.ConstraintType.PrimaryKey || first.Type == Constraint.ConstraintType.Unique;
            bool secondIsKey = second.Type == Constraint.ConstraintType.PrimaryKey || second.Type == Constraint.ConstraintType.Unique;
            if (!firstIsKey || !secondIsKey) return Constraint.Compare(first, second);
            return first.Type == second.Type && ConstraintColumns.Compare(first.Columns, second.Columns) &&
                SameMemoryIndex(first.Index, second.Index);
        }

        private static bool Same(ISchemaBase a, ISchemaBase b)
        {
            if (a is Index && b is Index) return SameMemoryIndex((Index)a, (Index)b);
            if (a is Constraint && b is Constraint) return SameConstraint((Constraint)a, (Constraint)b);
            if (a is Column && b is Column)
            {
                var first = (Column)a; var second = (Column)b;
                return Column.Compare(first, second) && first.IsRowGuid == second.IsRowGuid && SameDefault(first, second);
            }
            return a.ToSql() == b.ToSql();
        }

        private static bool SameSet<T>(IEnumerable<T> first, IEnumerable<T> second) where T : ISchemaBase
        {
            return first.Count() == second.Count() && first.All(a => second.Any(b => SameIdentity(a, b) && Same(a, b)));
        }

        public static bool SameDefinition(Table first, Table second)
        {
            return first.IsMemoryOptimized == second.IsMemoryOptimized && first.Durability == second.Durability &&
                SameSet(first.Columns, second.Columns) && SameSet(first.Indexes, second.Indexes) && SameSet(first.Constraints, second.Constraints) &&
                (!((Database)second.Parent).Options.Ignore.FilterColumnOrder || first.Columns.Select(c => c.Name).SequenceEqual(second.Columns.Select(c => c.Name), NameComparer(second)));
        }

        private static bool OnlyBucketChanged(ISchemaBase a, ISchemaBase b)
        {
            if (a == null || b == null) return false;
            var first = a as Index ?? (a is Constraint ? ((Constraint)a).Index : null);
            var second = b as Index ?? (b is Constraint ? ((Constraint)b).Index : null);
            if (first == null || second == null || first.Type != Index.IndexTypeEnum.Hash ||
                second.Type != Index.IndexTypeEnum.Hash || first.BucketCount == second.BucketCount) return false;
            var copy = (ISchemaBase)a.Clone(a.Parent);
            var copyIndex = copy as Index ?? ((Constraint)copy).Index;
            copyIndex.BucketCount = second.BucketCount;
            return Same(copy, b);
        }

        private static IEnumerable<T> Changed<T>(IEnumerable<T> current, IEnumerable<T> other,
            ICollection<ISchemaBase> selection) where T : ISchemaBase
        {
            return current.Where(c => Selected(c, selection) && !other.Any(o => SameIdentity(o, c) && Same(c, o)));
        }

        private static bool OnlyDefaultConstraintChanges(Table table, ICollection<ISchemaBase> selection)
        {
            var original = table.OriginalTable;
            if (table.Status != ObjectStatus.Alter || original == null ||
                table.IsMemoryOptimized != original.IsMemoryOptimized || table.Durability != original.Durability)
                return false;
            if (((Database)table.Parent).Options.Ignore.FilterColumnOrder &&
                !original.Columns.Select(c => c.Name).SequenceEqual(table.Columns.Select(c => c.Name), NameComparer(table)))
                return false;
            if (Changed(table.Indexes, original.Indexes, selection).Any() || Changed(original.Indexes, table.Indexes, selection).Any() ||
                Changed(table.Constraints, original.Constraints, selection).Any() || Changed(original.Constraints, table.Constraints, selection).Any())
                return false;

            bool defaultChanged = false;
            foreach (var column in table.Columns.Where(c => Selected(c, selection)))
            {
                var old = original.Columns.FirstOrDefault(c => SameIdentity(c, column));
                if (old == null || !Column.Compare(old, column) || old.IsRowGuid != column.IsRowGuid) return false;
                if (!SameDefault(old, column)) defaultChanged = true;
            }
            if (original.Columns.Where(c => Selected(c, selection)).Any(c => !table.Columns.Any(n => SameIdentity(n, c))))
                return false;
            return defaultChanged;
        }

        public static IList<string> BlockingReasons(Table table, ICollection<ISchemaBase> selection)
        {
            var reasons = new List<string>();
            if (!IsMemoryTable(table) || table.Status == ObjectStatus.Original) return reasons;
            var database = (Database)table.Parent;
            var info = database.Info;
            Action<string> block = reason => reasons.Add(table.FullName + ": " + reason);
            var filters = database.Options.Ignore;
            if (!filters.FilterTable || !filters.FilterIndex || !filters.FilterConstraint ||
                !filters.FilterConstraintPK || !filters.FilterConstraintUK || !filters.FilterConstraintFK || !filters.FilterConstraintCheck)
                block("memory table scripting requires complete column, index and constraint metadata; enable those comparison options.");
            if (info == null || !info.SupportsMemoryOptimizedTables) block("destination does not support In-Memory OLTP (SQL Server 2014+ required).");
            if (table.Status != ObjectStatus.Drop && (info == null || !info.HasMemoryOptimizedFileGroup))
                block("destination needs a MEMORY_OPTIMIZED_DATA filegroup with an online container; ask the administrator to prepare it.");
            var original = table.OriginalTable;
            if (table.Status == ObjectStatus.Alter && original == null) block("original table definition is missing.");
            if (original != null && original.IsMemoryOptimized != table.IsMemoryOptimized) block("conversion between disk and memory requires recreation; automatic data migration is disabled.");
            if (original != null && original.Durability != table.Durability) block("changing DURABILITY requires recreation.");
            if (table.HasState(ObjectStatus.Rebuild) || table.HasState(ObjectStatus.RebuildDependencies)) block("table reconstruction is disabled for memory-optimized tables.");
            if (table.Status != ObjectStatus.Create && table.SchemaBoundDependents.Count > 0 &&
                !OnlyDefaultConstraintChanges(table, selection))
                block("schema-bound dependencies must be handled first: " + string.Join(", ", table.SchemaBoundDependents));
            if (table.Status == ObjectStatus.Drop) return reasons;
            if (table.Durability != "SCHEMA_ONLY" && table.Durability != "SCHEMA_AND_DATA") block("unknown durability metadata.");
            if (table.Durability == "SCHEMA_AND_DATA" && !table.Constraints.Any(c => c.Type == Constraint.ConstraintType.PrimaryKey)) block("durable memory-optimized tables require a primary key.");
            if (!table.Indexes.Any() && !table.Constraints.Any(c => c.Type == Constraint.ConstraintType.PrimaryKey || c.Type == Constraint.ConstraintType.Unique)) block("memory-optimized tables require at least one index.");
            var indexes = table.Indexes.Concat(table.Constraints.Where(c => c.Type == Constraint.ConstraintType.PrimaryKey || c.Type == Constraint.ConstraintType.Unique).Select(c => c.Index)).ToList();
            foreach (var index in indexes)
            {
                if (index.Type != Index.IndexTypeEnum.Hash && index.Type != Index.IndexTypeEnum.Nonclustered) block("unsupported memory index type on " + index.Name + ".");
                if (index.Type == Index.IndexTypeEnum.Hash && (index.BucketCount < 1 || index.BucketCount > 1073741824)) block("invalid HASH bucket count on " + index.Name + ".");
            }
            if (info != null && info.MajorVersion < 14 && indexes.Count > 8) block("this destination version allows at most eight memory indexes.");
            if (info != null && info.MajorVersion < 13 && table.Constraints.Any(c => c.Type == Constraint.ConstraintType.Check || c.Type == Constraint.ConstraintType.ForeignKey)) block("CHECK/FOREIGN KEY constraints require SQL Server 2016+ for memory tables.");
            if (original != null && table.Status != ObjectStatus.Original)
            {
                var expectedOrder = original.Columns.Where(c => table.Columns.Any(n => SameIdentity(n, c))).Select(c => c.Name)
                    .Concat(table.Columns.Where(c => !original.Columns.Any(o => SameIdentity(o, c))).Select(c => c.Name));
                if (database.Options.Ignore.FilterColumnOrder && !expectedOrder.SequenceEqual(table.Columns.Select(c => c.Name), NameComparer(table)))
                    block("changing column order requires table reconstruction.");
                bool changed = Changed(table.Columns, original.Columns, selection).Any() || Changed(original.Columns, table.Columns, selection).Any() ||
                    Changed(table.Indexes, original.Indexes, selection).Any() || Changed(original.Indexes, table.Indexes, selection).Any() ||
                    Changed(table.Constraints, original.Constraints, selection).Any() || Changed(original.Constraints, table.Constraints, selection).Any();
                if (changed && (info == null || !info.SupportsMemoryOptimizedAlter)) block("ALTER of memory-optimized tables requires SQL Server 2016+; recreation is disabled.");
                foreach (var column in Changed(table.Columns, original.Columns, selection))
                {
                    var old = original.Columns.FirstOrDefault(c => SameIdentity(c, column));
                    if (old != null && (old.IsIdentity != column.IsIdentity || old.IdentitySeed != column.IdentitySeed || old.IdentityIncrement != column.IdentityIncrement || old.IsComputed != column.IsComputed || old.ComputedFormula != column.ComputedFormula))
                        block("column " + column.Name + " requires reconstruction.");
                    if (old == null && !column.IsNullable && column.DefaultConstraint == null)
                        block("new NOT NULL column " + column.Name + " needs an explicit DEFAULT to preserve existing rows.");
                }
            }
            return reasons;
        }

        public static void Validate(Database database, ICollection<ISchemaBase> selection)
        {
            var reasons = database.Tables.Where(t => IncludesTable(t, selection)).SelectMany(t =>
                BlockingReasons(t, Selected(t, selection) ? new List<ISchemaBase>() : selection)).Distinct().ToList();
            if (reasons.Count > 0) throw new ScriptGenerationException(reasons);
        }

        public static SQLScriptList Diff(Table table, ICollection<ISchemaBase> selection)
        {
            var reasons = BlockingReasons(table, selection);
            if (reasons.Count > 0) throw new ScriptGenerationException(reasons);
            var result = new SQLScriptList();
            if (table.Status == ObjectStatus.Original) return result;
            if (table.Status == ObjectStatus.Create) { result.Add(Create(table), table.DependenciesCount, ScriptAction.AddTable); return result; }
            if (table.Status == ObjectStatus.Drop) { result.Add(table.ToSqlDrop(), table.DependenciesCount, ScriptAction.DropTable); return result; }
            var old = table.OriginalTable;
            if (old == null) throw new ScriptGenerationException(new[] { table.FullName + ": original table definition is missing." });
            var sql = new StringBuilder();
            Action<string> alter = clause => sql.Append("ALTER TABLE ").Append(table.FullName).Append(" ").Append(clause).Append(";\r\nGO\r\n");
            var bucketChanges = table.Indexes.Cast<ISchemaBase>().Concat(table.Constraints).Where(c =>
                Selected(c, selection) && OnlyBucketChanged(c, old.Indexes.Cast<ISchemaBase>().Concat(old.Constraints).FirstOrDefault(o => o.ObjectType == c.ObjectType && SameIdentity(o, c)))).ToList();
            foreach (var item in bucketChanges)
            {
                var index = item as Index ?? ((Constraint)item).Index;
                alter("ALTER INDEX " + Quote(index.Name) + " REBUILD WITH (BUCKET_COUNT = " + index.BucketCount + ")");
            }
            Func<ISchemaBase, bool> notBucket = item => !bucketChanges.Any(c => SameIdentity(c, item));
            // Remove dependent indexes/constraints before altering columns, and restore them afterwards.
            foreach (var c in Changed(old.Constraints, table.Constraints, selection).Where(item => notBucket(item))) alter("DROP CONSTRAINT " + Quote(c.Name));
            foreach (var i in Changed(old.Indexes, table.Indexes, selection).Where(item => notBucket(item))) alter("DROP INDEX " + Quote(i.Name));
            foreach (var c in Changed(old.Columns, table.Columns, selection).Where(c => c.DefaultConstraint != null))
                alter("DROP CONSTRAINT " + Quote(c.DefaultConstraint.Name));
            foreach (var c in Changed(old.Columns, table.Columns, selection).Where(c => !table.Columns.Any(n => SameIdentity(n, c)))) alter("DROP COLUMN " + Quote(c.Name));
            foreach (var c in Changed(table.Columns, old.Columns, selection))
            {
                bool exists = old.Columns.Any(o => SameIdentity(o, c));
                alter(exists ? "ALTER COLUMN " + c.ToSql(false) : "ADD " + c.ToSql() + (c.DefaultConstraint != null ? " WITH VALUES" : ""));
                if (exists && c.DefaultConstraint != null)
                    alter("ADD CONSTRAINT " + Quote(c.DefaultConstraint.Name) + " DEFAULT " + c.DefaultConstraint.Definition + " FOR " + Quote(c.Name));
            }
            foreach (var c in Changed(table.Constraints, old.Constraints, selection).Where(item => notBucket(item))) alter("ADD " + ConstraintDefinition(c));
            foreach (var i in Changed(table.Indexes, old.Indexes, selection).Where(item => notBucket(item))) alter("ADD " + IndexDefinition(i));
            result.Add(sql.ToString(), table.DependenciesCount, ScriptAction.AlterTable);
            return result;
        }
    }
}
