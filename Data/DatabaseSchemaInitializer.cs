using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Data
{
    public class DatabaseSchemaInitializer : IDatabaseSchemaInitializer
    {
        private readonly NoteKeeperContext _context;
        private readonly ILogger<DatabaseSchemaInitializer> _logger;

        public DatabaseSchemaInitializer(
            NoteKeeperContext context,
            ILogger<DatabaseSchemaInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Applying pending Entity Framework migrations.");
            await _context.Database.MigrateAsync(cancellationToken);
        }
    }
}
