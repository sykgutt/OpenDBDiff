using OpenDBDiff.Abstractions.Schema.Misc;
using OpenDBDiff.SqlServer.Schema.Generates.SQLCommands;
using OpenDBDiff.SqlServer.Schema.Model;
using OpenDBDiff.SqlServer.Schema.Options;
using System;
using System.Data.SqlClient;
using System.Globalization;
#if DEBUG
#endif

namespace OpenDBDiff.SqlServer.Schema.Generates
{
    public class GenerateDatabase
    {
        private string connectioString;
        private SqlOption objectFilter;

        public bool UseDefaultVersionOnVersionParseError { get; private set; }

        /// <summary>
        /// Constructor de la clase.
        /// </summary>
        /// <param name="connectioString">Connection string de la base</param>
        public GenerateDatabase(string connectioString, SqlOption filter)
        {
            this.connectioString = connectioString;
            this.objectFilter = filter;
        }

        public DatabaseInfo Get(Database database)
        {
            DatabaseInfo item = new DatabaseInfo();
            using (SqlConnection conn = new SqlConnection(connectioString))
            {
                using (SqlCommand command = new SqlCommand(DatabaseSQLCommand.GetVersion(database), conn))
                {
                    conn.Open();

                    item.Server = conn.DataSource;
                    item.Database = conn.Database;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string versionValue = reader["Version"] as string;
                            try
                            {
                                // used to use the decimal as well when Azure was 10.25
                                var version = new Version(versionValue);
                                item.ProductVersion = versionValue;
                                item.VersionNumber = float.Parse(String.Format("{0}.{1}", version.Major, version.Minor), CultureInfo.InvariantCulture);

                                if (reader.FieldCount > 1 && !reader.IsDBNull(1))
                                {
                                    int edition;
                                    if (int.TryParse(reader[1].ToString(), out edition)
                                        && Enum.IsDefined(typeof(DatabaseInfo.SQLServerEdition), edition))
                                    {
                                        item.SetEdition((DatabaseInfo.SQLServerEdition)edition);
                                    }
                                }

                            }
                            catch (Exception notAGoodIdeaToCatchAllErrors)
                            {
                                var exception = new SchemaException(
                                    String.Format("Error parsing ProductVersion. ({0})", versionValue ?? "[null]")
                                    , notAGoodIdeaToCatchAllErrors);

                                if (!UseDefaultVersionOnVersionParseError)
                                {
                                    throw exception;
                                }
                            }
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(DatabaseSQLCommand.Get(item.Version, item.Edition, database), conn))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            item.Collation = reader["Collation"].ToString();
                            item.HasFullTextEnabled = ((int)reader["IsFulltextEnabled"]) == 1;
                        }
                    }
                }

            }

            if (item.MajorVersion >= 12)
            {
                using (var conn = new SqlConnection(connectioString))
                using (var command = new SqlCommand(@"SELECT CONVERT(int, SERVERPROPERTY('IsXTPSupported')) AS Supported,
                    CASE WHEN EXISTS (SELECT 1 FROM sys.filegroups fg JOIN sys.database_files f ON f.data_space_id=fg.data_space_id
                    WHERE fg.type='FX' AND f.state=0) THEN 1 ELSE 0 END AS HasContainer", conn))
                {
                    conn.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        item.IsXtpSupported = !reader.IsDBNull(0) && reader.GetInt32(0) == 1;
                        item.HasMemoryOptimizedFileGroup = reader.GetInt32(1) == 1;
                    }
                }
            }
            return item;
        }
    }
}
