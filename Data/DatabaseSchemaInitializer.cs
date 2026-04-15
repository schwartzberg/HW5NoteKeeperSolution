using Microsoft.EntityFrameworkCore;

namespace HW5NoteKeeperSolution.Data
{
    /// <summary>
    /// EF Core implementation of <see cref="IDatabaseSchemaInitializer"/> that applies
    /// pending migrations at application startup using <c>MigrateAsync</c>.
    /// </summary>
    public class DatabaseSchemaInitializer : IDatabaseSchemaInitializer
    {
        private readonly NoteKeeperContext _context;
        private readonly ILogger<DatabaseSchemaInitializer> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DatabaseSchemaInitializer"/>.
        /// </summary>
        /// <param name="context">The EF Core context used to apply migrations.</param>
        /// <param name="logger">Logger for migration progress messages.</param>
        public DatabaseSchemaInitializer(
            NoteKeeperContext context,
            ILogger<DatabaseSchemaInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Applying pending Entity Framework migrations.");
            await _context.Database.MigrateAsync(cancellationToken);
        }
    }
}
