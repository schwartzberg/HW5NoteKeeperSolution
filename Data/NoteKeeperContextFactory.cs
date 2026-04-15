using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HW5NoteKeeperSolution.Data
{
    public class NoteKeeperContextFactory : IDesignTimeDbContextFactory<NoteKeeperContext>
    {
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
