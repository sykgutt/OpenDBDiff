using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBDiff.SqlServer.Ui.Util;

namespace OpenDBDiff.Tests.Settings
{
    [TestClass]
    public class SqlConnectionStringCompatibilityTests
    {
        [TestMethod]
        public void Parse_SpacedTrustCertificateAlias_PreservesConnectionValues()
        {
            var result = SqlConnectionStringCompatibility.Parse("Server=localhost;Database=Sample;User ID=test;Password=\"contains;Trust Server Certificate=false\";Trust Server Certificate=True;");

            Assert.IsTrue(result.TrustServerCertificate);
            Assert.AreEqual("localhost", result.DataSource);
            Assert.AreEqual("Sample", result.InitialCatalog);
            Assert.AreEqual("contains;Trust Server Certificate=false", result.Password);
        }

        [TestMethod]
        public void Parse_StandardKeyword_PreservesFalseValue()
        {
            var result = SqlConnectionStringCompatibility.Parse("Server=localhost;Integrated Security=True;TrustServerCertificate=False;");

            Assert.IsFalse(result.TrustServerCertificate);
            Assert.IsTrue(result.IntegratedSecurity);
        }
    }
}
