namespace HW5NoteKeeperSolution.Data
{
    /// <summary>
    /// Defines the contract for applying pending EF Core migrations to the database at application startup.
    /// </summary>
    public interface IDatabaseSchemaInitializer
    {
        /// <summary>
        /// Applies any pending EF Core migrations to the target database.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
