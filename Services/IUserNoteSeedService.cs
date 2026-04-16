namespace HW5NoteKeeper.Services
{
    /// <summary>
    /// Defines the contract for idempotent, per-user seed-data initialisation.
    /// The implementation creates the default notes and uploads seed attachments on the
    /// first authenticated request for each user.
    /// </summary>
    public interface IUserNoteSeedService
    {
        /// <summary>
        /// Ensures that the default seed notes and blob attachments exist for the given user.
        /// This method is idempotent and uses an in-memory cache to avoid redundant database queries.
        /// </summary>
        /// <param name="userRealmId">The Entra object identifier of the authenticated user.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task EnsureSeedDataAsync(string userRealmId, CancellationToken cancellationToken = default);
    }
}
