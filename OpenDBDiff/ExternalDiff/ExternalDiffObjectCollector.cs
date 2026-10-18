using OpenDBDiff.Abstractions.Schema;
using OpenDBDiff.Abstractions.Schema.Attributes;
using OpenDBDiff.Abstractions.Schema.Model;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDBDiff.ExternalDiff
{
    public class ExternalDiffPair
    {
        public string RelativePath { get; set; }

        public string NewSql { get; set; }

        public string OldSql { get; set; }
    }

    public static class ExternalDiffObjectCollector
    {
        public static IEnumerable<ISchemaBase> GetTopLevelObjects(ISchemaBase database)
        {
            if (database == null)
                yield break;

            foreach (var property in database.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attributes = property.GetCustomAttributes(typeof(SchemaNodeAttribute), true);
                if (attributes.Length == 0)
                    continue;

                var items = property.GetValue(database, null) as IList;
                if (items == null)
                    continue;

                foreach (ISchemaBase item in items)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        public static List<ExternalDiffPair> GetDifferences(IDatabase leftDatabase, IDatabase rightDatabase)
        {
            var pairs = new List<ExternalDiffPair>();
            if (leftDatabase == null)
                return pairs;

            foreach (var item in GetTopLevelObjects(leftDatabase))
            {
                if (item.Status == ObjectStatus.Original)
                    continue;

                var rightItem = rightDatabase?.Find(item.FullName);
                var newSql = item.Status != ObjectStatus.Drop ? SafeSql(item) : null;
                var oldSql = item.Status != ObjectStatus.Create
                    ? SafeSql(rightItem) ?? SafeSql(item)
                    : null;

                if (string.IsNullOrEmpty(newSql) && string.IsNullOrEmpty(oldSql))
                    continue;

                pairs.Add(new ExternalDiffPair
                {
                    RelativePath = ExternalDiffFileNames.RelativePath(item.ObjectType, item.FullName),
                    NewSql = newSql,
                    OldSql = oldSql
                });
            }

            return pairs;
        }

        private static string SafeSql(ISchemaBase item)
        {
            if (item == null)
                return null;

            try
            {
                return item.ToSql();
            }
            catch
            {
                return null;
            }
        }
    }
}
