using System.Data.Common;
using System.Data.SqlClient;

namespace OpenDBDiff.SqlServer.Ui.Util
{
    internal static class SqlConnectionStringCompatibility
    {
        internal static SqlConnectionStringBuilder Parse(string value)
        {
            var settings = new DbConnectionStringBuilder { ConnectionString = value };
            const string spacedKeyword = "Trust Server Certificate";
            if (settings.ContainsKey(spacedKeyword))
            {
                var trustCertificate = settings[spacedKeyword];
                settings.Remove(spacedKeyword);
                settings["TrustServerCertificate"] = trustCertificate;
            }
            return new SqlConnectionStringBuilder(settings.ConnectionString);
        }
    }
}
