using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Tests that verify the connection string sanitization in Program.cs prevents
    /// the "Pick an account" popup and the AccessToken/UserID conflict error.
    /// </summary>
    public class ConnectionStringSanitizationTests
    {
        /// <summary>
        /// Reproduces the exact sanitization logic from Program.cs.
        /// If Program.cs changes, this helper must be updated to match.
        /// </summary>
        private static string SanitizeConnectionString(string connectionString)
        {
            var csBuilder = new SqlConnectionStringBuilder(connectionString);
            csBuilder.Remove("Authentication");
            csBuilder.Remove("User ID");
            csBuilder.Remove("UID");
            csBuilder.Remove("Password");
            csBuilder.Remove("PWD");
            return csBuilder.ToString();
        }

        [Fact]
        public void Sanitize_RemovesAuthenticationActiveDirectoryDefault()
        {
            string input = "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;" +
                           "Encrypt=True;Authentication=Active Directory Default;";

            string result = SanitizeConnectionString(input);

            result.Should().NotContainEquivalentOf("Authentication");
            result.Should().Contain("myserver.database.windows.net");
        }

        [Fact]
        public void Sanitize_RemovesActiveDirectoryInteractiveAndUserId()
        {
            string input = "Data Source=myserver.database.windows.net,1433;Initial Catalog=mydb;" +
                           "User ID=admin@tenant.onmicrosoft.com;" +
                           "Authentication=ActiveDirectoryInteractive;";

            string result = SanitizeConnectionString(input);

            result.Should().NotContainEquivalentOf("Authentication");
            result.Should().NotContainEquivalentOf("User ID");
            result.Should().Contain("myserver.database.windows.net");
        }

        [Fact]
        public void Sanitize_RemovesPasswordAndUid()
        {
            string input = "Server=myserver;Database=mydb;UID=sa;PWD=secret123;";

            string result = SanitizeConnectionString(input);

            result.Should().NotContain("secret123");
            var rebuilt = new SqlConnectionStringBuilder(result);
            rebuilt.UserID.Should().BeEmpty();
            rebuilt.Password.Should().BeEmpty();
        }

        [Fact]
        public void Sanitize_PreservesServerAndDatabase()
        {
            string input = "Server=tcp:sql-cscie94-2026-ps.database.windows.net,1433;" +
                           "Initial Catalog=sqldb-cscie94-2026_hw5;Encrypt=True;" +
                           "TrustServerCertificate=False;Connection Timeout=120;" +
                           "Authentication=Active Directory Default;";

            string result = SanitizeConnectionString(input);

            var rebuilt = new SqlConnectionStringBuilder(result);
            rebuilt.DataSource.Should().Contain("sql-cscie94-2026-ps.database.windows.net");
            rebuilt.InitialCatalog.Should().Be("sqldb-cscie94-2026_hw5");
            rebuilt.Encrypt.Should().Be(SqlConnectionEncryptOption.Mandatory);
            rebuilt.ConnectTimeout.Should().Be(120);
        }

        [Fact]
        public void SqlConnection_AccessToken_CanBeSet_AfterSanitization()
        {
            string input = "Server=myserver;Database=mydb;" +
                           "User ID=admin@tenant.onmicrosoft.com;" +
                           "Authentication=ActiveDirectoryInteractive;";

            string sanitized = SanitizeConnectionString(input);

            using var connection = new SqlConnection(sanitized);
            var setToken = () => connection.AccessToken = "fake-token-for-test";

            setToken.Should().NotThrow(
                "setting AccessToken must succeed after Authentication/UserID are stripped");
        }

        [Fact]
        public void SqlConnection_AccessToken_Throws_WithoutSanitization()
        {
            string input = "Server=myserver;Database=mydb;" +
                           "User ID=admin@tenant.onmicrosoft.com;" +
                           "Authentication=ActiveDirectoryInteractive;";

            using var connection = new SqlConnection(input);
            var setToken = () => connection.AccessToken = "fake-token-for-test";

            setToken.Should().Throw<InvalidOperationException>(
                "this reproduces the exact error the user saw before the fix");
        }
    }
}
