namespace HW5NoteKeeperSolution.Services
{
    public interface IUserNoteSeedService
    {
        Task EnsureSeedDataAsync(string userRealmId, CancellationToken cancellationToken = default);
    }
}
