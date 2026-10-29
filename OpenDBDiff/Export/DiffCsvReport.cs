using OpenDBDiff.Abstractions.Schema.Model;
using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Attributes;
using OpenDBDiff.SqlServer.Schema.Model;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenDBDiff.Export
{
    internal sealed class DiffCsvRow
    {
        public DiffCsvRow(string objectType, string schema, string name, string fullName, string difference, string action)
        {
            ObjectType = objectType;
            Schema = schema;
            Name = name;
            FullName = fullName;
            Difference = difference;
            Action = action;
        }

        public string ObjectType { get; private set; }
        public string Schema { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public string Difference { get; private set; }
        public string Action { get; private set; }
    }

    internal sealed class DiffCsvReport
    {
        public DiffCsvReport(ISchemaBase comparison, string sourceName, string targetName)
        {
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            if (string.IsNullOrWhiteSpace(targetName)) throw new ArgumentException("El nombre de la base target no puede estar vacío.", nameof(targetName));
            if (string.IsNullOrWhiteSpace(sourceName)) throw new ArgumentException("El nombre de la base source no puede estar vacío.", nameof(sourceName));

            var rows = new List<DiffCsvRow>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in comparison.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var group = property.GetCustomAttributes(typeof(SchemaNodeAttribute), true)
                    .Cast<SchemaNodeAttribute>().FirstOrDefault();
                if (group == null) continue;
                var items = property.GetValue(comparison, null) as IEnumerable;
                if (items == null) continue;

                foreach (ISchemaBase item in items)
                {
                    if (item == null || !identities.Add(item.ObjectType + "\0" + item.FullName)) continue;
                    var table = item as Table;
                    var objectType = table != null && table.IsMemoryOptimized ? "Memory-Optimized Tables" : group.Name;
                    rows.Add(new DiffCsvRow(objectType, item.Owner ?? "", item.Name ?? "", item.FullName, Difference(item.Status), Action(item.Status, sourceName, targetName)));
                }
            }
            Rows = rows.OrderBy(r => r.ObjectType, StringComparer.Ordinal)
                .ThenBy(r => r.Schema, StringComparer.Ordinal)
                .ThenBy(r => r.Name, StringComparer.Ordinal).ToList().AsReadOnly();

            var invalid = Path.GetInvalidFileNameChars();
            var name = new string(targetName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            SuggestedFileName = "Diff" + name + ".csv";
        }

        public ReadOnlyCollection<DiffCsvRow> Rows { get; private set; }
        public string SuggestedFileName { get; private set; }

        public void Save(string path)
        {
            var content = new StringBuilder("Tipo de objeto,Esquema,Nombre del objeto,Nombre completo,Tipo de diferencia,Acción\r\n");
            foreach (var row in Rows)
            {
                var fields = new[] { row.ObjectType, row.Schema, row.Name, row.FullName, row.Difference, row.Action };
                content.Append(string.Join(",", fields.Select(Escape))).Append("\r\n");
            }

            var destination = Path.GetFullPath(path);
            var temporary = Path.Combine(Path.GetDirectoryName(destination), "." + Guid.NewGuid().ToString("N") + ".csv.tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(true), 1024, true))
                        writer.Write(content.ToString());
                    stream.Flush(true);
                }
                if (File.Exists(destination))
                    File.Replace(temporary, destination, null);
                else
                    File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private static string Escape(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Action(ObjectStatus status, string source, string target)
        {
            source = "[" + source + "]";
            target = "[" + target + "]";
            if (status == ObjectStatus.Original) return $"Sin acción: objeto sin cambios entre BD source {source} y BD target {target}.";
            if (status == ObjectStatus.Create) return $"Crear en BD target {target}: el objeto existe en BD source {source} y falta en BD target.";
            if (status == ObjectStatus.Drop) return $"Eliminar de BD target {target}: el objeto existe en BD target y falta en BD source {source}.";
            if ((status & ObjectStatus.Rebuild) != 0) return $"Revisar la reconstrucción del objeto en BD target {target} para coincidir con BD source {source}.";
            if ((status & ObjectStatus.AlterWhitespace) != 0) return $"Revisar diferencias de formato entre BD source {source} y BD target {target}; contenido equivalente según las opciones de comparación.";
            return $"Actualizar la definición o estructura en BD target {target} para coincidir con BD source {source}.";
        }

        private static string Difference(ObjectStatus status)
        {
            // Keep the same precedence as the object nodes, including combined flags.
            if (status == ObjectStatus.Original) return "unchanged item";
            if (status == ObjectStatus.Create) return "new item";
            if (status == ObjectStatus.Drop) return "missing item";
            if ((status & ObjectStatus.Rebuild) != 0) return "changed item data";
            if ((status & ObjectStatus.AlterWhitespace) != 0) return "changed item view";
            return "changed item data";
        }
    }
}
