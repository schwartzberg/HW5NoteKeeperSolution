using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HW5NoteKeeper.Data
{
    /// <summary>
    /// Design-time factory for <see cref="NoteKeeperContext"/>, used by EF Core tooling
    /// (<c>dotnet ef migrations add</c>, <c>Update-Database</c>, etc.) when no host is running.
    /// Reads connection settings from <c>appsettings.json</c>, <c>appsettings.Development.json</c>,
    /// user secrets, and environment variables.
    /// </summary>
    public class NoteKeeperContextFactory : IDesignTimeDbContextFactory<NoteKeeperContext>
    {
        /// <summary>
        /// Creates a configured <see cref="NoteKeeperContext"/> for design-time EF Core tooling.
        /// </summary>
        /// <param name="args">Command-line arguments passed by EF tooling (not used).</param>
        /// <returns>A <see cref="NoteKeeperContext"/> connected to the configured SQL Server database.</returns>
        public NoteKeeperContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

            DbContextOptions<NoteKeeperContext> options = new DbContextOptionsBuilder<NoteKeeperContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new NoteKeeperContext(options);
        }
    }
}
