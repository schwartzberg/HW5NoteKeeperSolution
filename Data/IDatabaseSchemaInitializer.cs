namespace HW5NoteKeeperSolution.Data
{
    public interface IDatabaseSchemaInitializer
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
